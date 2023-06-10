
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using UdpQuickShare.Protocols;


namespace UdpQuickShare.Clients
{
    public class MyUdpClient
    {
        readonly UdpClient udpClient;
        readonly IEncoder encoder;
        readonly IDecoder decoder;
        readonly ByteArrayBuffer sendBuffer;

        IPEndPoint currentIp;
        readonly int port;



        public IPEndPoint CurrentIp=>currentIp;
        public event EventHandler CurrentIpChanged;
        public event EventHandler<UdpRecievedEventArgs> OnRecieved;

        bool keepRecieving;
        readonly Queue<UdpReceiveResult> recieveQueue;
        bool queueRunning;
        readonly object addQueueLock=new();

        TaskCompletionSource<(IPEndPoint Sender, ISegement Segement)> recieveTask;

        static void Log(string message)
        {
            App.Log(message);
        }

        static void Log(object value)
        {
            App.Log(value);
        }
        public MyUdpClient(IEncoder encoder,IDecoder decoder,int port,int maxBufferSize)
        {
            udpClient= new UdpClient(port);
            this.port= port;
            udpClient.EnableBroadcast = true;
            udpClient.MulticastLoopback = false;
            this.encoder = encoder;
            this.decoder = decoder;
            sendBuffer=new ByteArrayBuffer(new byte[maxBufferSize]);
            recieveQueue=new Queue<UdpReceiveResult>();
        }
        public async Task<bool> GetOwnIp()
        {
            try
            {
                if (currentIp != null)
                {
                    return true;
                }
                var timeout = DateTime.Now.AddSeconds(10);
                var value = new Random().Next(0, 128);
                MyUdpClient.Log($"start get own ip");
                while (DateTime.Now < timeout)
                {
                    for (int i = 0; i < 5; i++)
                    {
                        _ = udpClient.SendAsync(new byte[] { (byte)value }, 1, new IPEndPoint(IPAddress.Broadcast, port));
                    }
                    var res = await udpClient.ReceiveAsync();
                    if (res.Buffer.Length == 1 && res.Buffer[0] == value)
                    {
                        currentIp = res.RemoteEndPoint;
                        MyUdpClient.Log($"get own ip {res.RemoteEndPoint} successfully");
                        CurrentIpChanged?.Invoke(this,new EventArgs());
                        return true;
                    }
                }
            }
            catch(Exception e)
            {
                MyUdpClient.Log(e);
            }
            MyUdpClient.Log($"not get own ip");
            return false;
        }


        public void StartRecieving()
        {
            keepRecieving = true;
            Task.Run(async () =>
            {
                MyUdpClient.Log($"keep recieing in udp client start");
                while(keepRecieving)
                {
                    try
                    {
                        using var source = new CancellationTokenSource();
                        source.CancelAfter(10000);
                        var res = await udpClient.ReceiveAsync(source.Token);
                        if (res.RemoteEndPoint.Equals(currentIp))
                        {
                            continue;
                        }
                        else
                        {
                            AddToQueue(res);
                        }
                    }
                    catch { }
                }
                MyUdpClient.Log($"stop keep recieing in udp client");
            });
        }

        void AddToQueue(UdpReceiveResult udpReceiveResult)
        {
            lock(addQueueLock)
            {
                recieveQueue.Enqueue(udpReceiveResult);
                if (!queueRunning)
                {
                    queueRunning = true;
                    RunQueue();
                }
            }
        }
        void RunQueue()
        {
            Task.Run(() =>
            {
                while(recieveQueue.Any())
                {
                    var res=recieveQueue.Dequeue();
                    var segement = decoder.Decode(new ByteArrayBuffer(res.Buffer));
                    if (segement != null)
                    {
                        if (segement.SegementType == SegementType.Reply&&recieveTask!=null)
                        {
                            recieveTask.TrySetResult((res.RemoteEndPoint, segement));
                            recieveTask = null;
                        }
                        if(segement is CommandSegement command)
                        {
                            MyUdpClient.Log($"recieve commmand:{command.Command}-{command.Value} from {res.RemoteEndPoint}");
                        }
                        else
                        {
                            MyUdpClient.Log($"recieve segement:{segement.GetType().Name} from {res.RemoteEndPoint}");
                        }
                        OnRecieved?.Invoke(this, new UdpRecievedEventArgs(res.RemoteEndPoint, segement));
                    }
                }
                queueRunning = false;
            });
        }
        public void StopRecieving()
        {
            keepRecieving= false;
        }
        public Task SendAsync(ISegement segement,IPEndPoint endPoint, [CallerMemberName] string caller = "")
        {
            try
            {
                sendBuffer.Reset();
                encoder.Encode(sendBuffer, segement);
                if (segement is CommandSegement command)
                {
                    MyUdpClient.Log($"send commmand:{command.Command}-{command.Value} to {endPoint}");
                }
                else
                {
                    MyUdpClient.Log($"send {segement.GetType().Name} FileId : {segement.FileId} to {endPoint}");
                }
                return udpClient.SendAsync(sendBuffer.Data, sendBuffer.Count, endPoint);
            }
            catch (Exception e)
            {
                MyUdpClient.Log($"{caller} Send {segement.GetType()} FileId : {segement.FileId} to {endPoint} error:{e}");
            }
            return Task.CompletedTask;
        }

        public async Task<(IPEndPoint Sender,ISegement Segement)> SendForResultAsync(ISegement segement, IPEndPoint endPoint,int timeout = 1000)
        {
            int tryCount = 3;
            MyUdpClient.Log($"start send for result");
            while (tryCount-- > 0)
            {          
                await SendAsync(segement, endPoint);
                recieveTask = new TaskCompletionSource<(IPEndPoint Sender, ISegement Segement)>();
                var taskSource = recieveTask;
                var task2 = Task.Delay(timeout);
                await Task.WhenAny(taskSource.Task, task2).ConfigureAwait(false);
                if (taskSource.Task.IsCompleted)
                {
                    var segement0 = taskSource.Task.Result.Segement;
                    MyUdpClient.Log($"get result for send {segement0?.SegementType}[{segement0?.FileId}] from {taskSource.Task.Result.Sender}");
                    return taskSource.Task.Result;
                }
                else
                {
                    MyUdpClient.Log($"{3 - tryCount} times timeout");
                }
            }
            MyUdpClient.Log($"send for result not get result");
            return (null, null);
        }
    }
}
