
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
        TcpClient tcpClient;
        int bufferSize;
        public TcpFileSender(int bufferSize)
        {
            tcpClient = new TcpClient();
            this.bufferSize = bufferSize;
        }
        public void Log(string message) => App.Log(message);
        public void Dispose()
        {
            ((IDisposable)tcpClient).Dispose();
        }

        public async Task<bool> SendFileAsync(Stream stream,IPEndPoint endPoint,Action<long> sending,CancellationToken cancellation, int invokeDelta = 1000)
        {
            try
            {
                Log($"tcpclient waiting for connect");
                int tryCount = 3;
                while (tryCount-- > 0)
                {
                    try
                    {
                        await tcpClient.ConnectAsync(endPoint, cancellation);
                    }
                    catch(Exception ex)
                    {
                        //Log($"connect ex:{ex}");
                        Log($"{3 - tryCount} connect failed");
                    }
                }
                if (tcpClient.Connected)
                {
                    Log($"tcpclient connected");
                }
                else
                {
                    Log($"tcpclient failed");
                    return false;
                }
                var buffer = new byte[bufferSize];
                DateTime lastTime = DateTime.Now;
                using (var netStream = tcpClient.GetStream())
                {
                    int readed = 0;
                    do
                    {
                        cancellation.ThrowIfCancellationRequested();
                        readed =await stream.ReadAsync(buffer);
                        await netStream.WriteAsync(buffer.AsMemory(0, readed));
                        await netStream.FlushAsync();
                        if (DateTime.Now - lastTime > TimeSpan.FromMilliseconds(invokeDelta))
                        {
                            sending?.Invoke(stream.Position);
                            lastTime = DateTime.Now;
                        }                     
                    }
                    while (readed > 0);
                    Log($"tcpclient completed");
                    return true;
                }
            }
            catch(Exception ex)
            {
                //Log($"sending file :{ex}");
            }
            Log($"tcpclient stopped");
            return false;
        }
    }
}
