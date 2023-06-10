
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using UdpQuickShare.Protocols;

namespace UdpQuickShare.Clients
{
    public class TcpFileSender:IDisposable
    {
        readonly TcpClient tcpClient;
        readonly int bufferSize;
        public TcpFileSender(int bufferSize)
        {
            tcpClient = new TcpClient();
            this.bufferSize = bufferSize;
        }

        public void Dispose()
        {
            ((IDisposable)tcpClient).Dispose();
            GC.SuppressFinalize(this);
        }

        public static void Log(string message) => App.Log(message);


        public async Task<bool> SendFileAsync(Stream stream,IPEndPoint endPoint,Action<long> sending,CancellationToken cancellation, int invokeDelta = 1000)
        {
            try
            {
                TcpFileSender.Log($"tcpclient waiting for connect");
                int tryCount = 3;
                while (tryCount-- > 0)
                {
                    try
                    {
                        await tcpClient.ConnectAsync(endPoint, cancellation);
                        
                    }
                    catch
                    {
                        //Log($"connect ex:{ex}");
                        TcpFileSender.Log($"{3 - tryCount} connect failed");
                        await Task.Delay(2000);
                    }
                }
                if (tcpClient.Connected)
                {
                    TcpFileSender.Log($"tcpclient connected");
                }
                else
                {
                    TcpFileSender.Log($"tcpclient failed");
                    return false;
                }
                var buffer = new byte[bufferSize];
                DateTime lastTime = DateTime.Now;
                var delta = TimeSpan.FromMilliseconds(invokeDelta);
                using (var netStream = tcpClient.GetStream())
                {
                    int readed = 0;
                    do
                    {
                        cancellation.ThrowIfCancellationRequested();
                        readed =stream.Read(buffer);
                        netStream.Write(buffer.AsSpan(0, readed));
                        netStream.Flush();
                        if (DateTime.Now - lastTime >delta)
                        {
                            sending?.Invoke(stream.Position);
                            lastTime = DateTime.Now;
                        }                     
                    }
                    while (readed > 0);
                    TcpFileSender.Log($"tcpclient completed");
                }
                tcpClient.Close();
                tcpClient.Dispose();
                return true;
            }
            catch(OperationCanceledException ex)
            {
                tcpClient.Close();
                tcpClient.Dispose();
            }
            catch(Exception ex)
            {
                TcpFileSender.Log($"sending file :{ex}");
            }

            TcpFileSender.Log($"tcpclient stopped");
            return false;
        }
    }
}
