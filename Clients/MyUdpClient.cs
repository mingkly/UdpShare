
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
        UdpClient udpClient;
        IEncoder encoder;
        IDecoder decoder;
        ByteArrayBuffer sendBuffer;
        IPEndPoint currentIp;
        int port;
        bool KeepRecieving;
        public IPEndPoint CurrentIp=>currentIp;
        public event EventHandler CurrentIpChanged;
        public event EventHandler<UdpRecievedEventArgs> OnRecieved;
        Queue<UdpReceiveResult> recieveQueue;
        bool queueRunning;
        TaskCompletionSource<(IPEndPoint Sender, ISegement Segement)> recieveTask;


        void Log(string message)
        {
            App.Log(message);
        }
        void Log(object value)
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
                Log($"start get own ip");
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
                        Log($"get own ip {res.RemoteEndPoint} successfully");
                        CurrentIpChanged?.Invoke(this,new EventArgs());
                        return true;
                    }
                }
            }
            catch(Exception e)
            {
                Log(e);
            }
            Log($"not get own ip");
            return false;
        }
        public void StartRecieving()
        {
            KeepRecieving = true;
            Task.Run(async () =>
            {
                Log($"keep recieing in udp client start");
                while(KeepRecieving)
                {
                    try
                    {
                        using (var source = new CancellationTokenSource())
                        {
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
                    }
                    catch { }
                }
                Log($"stop keep recieing in udp client");
            });
        }

        void AddToQueue(UdpReceiveResult udpReceiveResult)
        {
            recieveQueue.Enqueue(udpReceiveResult);
            if(!queueRunning)
            {
                queueRunning = true;
                RunQueue();
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
                            Log($"recieve commmand:{command.Command}-{command.Value} from {res.RemoteEndPoint}");
                        }
                        else
                        {
                            Log($"recieve segement:{segement.GetType().Name} from {res.RemoteEndPoint}");
                        }
                        OnRecieved?.Invoke(this, new UdpRecievedEventArgs(res.RemoteEndPoint, segement));
                    }
                }
                queueRunning = false;
            });
        }
        public void StopRecieving()
        {
            KeepRecieving= false;
        }
        public Task SendAsync(ISegement segement,IPEndPoint endPoint, [CallerMemberName] string caller = "")
        {
            try
            {
                sendBuffer.Reset();
                encoder.Encode(sendBuffer, segement);
                if (segement is CommandSegement command)
                {
                    Log($"send commmand:{command.Command}-{command.Value} to {endPoint}");
                }
                else
                {
                    Log($"send {segement.GetType().Name} FileId : {segement.FileId} to {endPoint}");
                }
                return udpClient.SendAsync(sendBuffer.Data, sendBuffer.Count, endPoint);
            }
            catch (Exception e)
            {
                Log($"{caller} Send {segement.GetType()} FileId : {segement.FileId} to {endPoint} error:{e}");
            }
            return Task.CompletedTask;
        }

        public async Task<(IPEndPoint Sender,ISegement Segement)> SendForResultAsync(ISegement segement, IPEndPoint endPoint,int timeout = 1000, [CallerMemberName] string caller="")
        {
            int tryCount = 3;
            Log($"start send for result");
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
                    Log($"get result for send {segement0?.SegementType}[{segement0?.FileId}] from {taskSource.Task.Result.Sender}");
                    return taskSource.Task.Result;
                }
                else
                {
                    Log($"{3 - tryCount} times timeout");
                }
            }
            Log($"send for result not get result");
            return (null, null);
        }
    }
}
