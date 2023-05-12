
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Cache;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace UdpQuickShare.Clients
{
    public class TcpFileReciever
    {
        TcpListener tcpListener;
        int bufferSize;
        public TcpFileReciever(int bufferSize,int port)
        {
            tcpListener = new TcpListener(IPAddress.Any, port);

            this.bufferSize = bufferSize;
        }
        public void Log(string message)=>App.Log(message);


        public async Task<bool> RecievingFile(Stream stream,Action<long> recieving,CancellationToken cancellationToken,int invokeDelta=1000)
        {
            try
            {
                Log($"tcpclient waiting for connect");
                tcpListener.Start();
                var client = await tcpListener.AcceptTcpClientAsync(cancellationToken);
                Log($"tcpclient connected");
                var buffer = new byte[bufferSize];
                if (client != null)
                {
                    int readed;
                    DateTime lastTime=DateTime.Now;
                    using var netStream = client.GetStream();
                    do
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        readed = await netStream.ReadAsync(buffer);
                        await stream.WriteAsync(buffer.AsMemory().Slice(0,readed));
                        if (DateTime.Now - lastTime > TimeSpan.FromMilliseconds(invokeDelta))
                        {
                            recieving?.Invoke(stream.Position);
                            lastTime=DateTime.Now;
                        }                      
                    } while (readed > 0);
                    tcpListener.Stop();
                    Log($"tcpclient completed");
                    return true;
                }

            }
            catch(Exception ex)
            {

                //Log($"recieve: {ex}");
            }
            Log($"tcpclient stopped");
            tcpListener.Stop();
            return false;
        }
    }
}
