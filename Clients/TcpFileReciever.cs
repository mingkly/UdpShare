
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
        readonly TcpListener tcpListener;
        readonly int bufferSize;
        public TcpFileReciever(int bufferSize,int port)
        {
            tcpListener = new TcpListener(IPAddress.Any, port);

            this.bufferSize = bufferSize;
        }
        public static void Log(string message)=>App.Log(message);


        public async Task<(bool CompletedNormal,long Readed)> RecievingFile(Stream stream,Action<long> recieving,CancellationToken cancellationToken,int invokeDelta=1000,long startPosition=0)
        {
            long totalReaded = startPosition;
            try
            {
                TcpFileReciever.Log($"tcpclient waiting for connect");
                tcpListener.Start();
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken,new CancellationTokenSource(6000).Token);
                var client = await tcpListener.AcceptTcpClientAsync(cts.Token);
                TcpFileReciever.Log($"tcpclient connected");
                var buffer = new byte[bufferSize];

                if (client != null)
                {
                    int readed;
                    DateTime lastTime=DateTime.Now;
                    using var netStream = client.GetStream();
                    do
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        readed = netStream.Read(buffer);
                        totalReaded += readed;
                        stream.Write(buffer.AsSpan(0,readed));
                        if (DateTime.Now - lastTime > TimeSpan.FromMilliseconds(invokeDelta))
                        {
                            recieving?.Invoke(totalReaded);
                            lastTime=DateTime.Now;
                        }
                    } while (readed > 0);
                    tcpListener.Stop();
                    TcpFileReciever.Log($"tcpclient completed");
                    return (true,totalReaded);
                }

            }
            catch(Exception ex)
            {
                TcpFileReciever.Log($"recieve: {ex}");
            }
            TcpFileReciever.Log($"tcpclient stopped");
            tcpListener.Stop();
            
            return (false,totalReaded);
        }
    }
}
