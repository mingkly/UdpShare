
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using UdpQuickShare.FileActions;
using UdpQuickShare.FileActions.FileSavers;


namespace UdpQuickShare.Clients
{
    public class MyTcpClient
    {
        readonly int port;
        readonly int maxBufferSize;
        readonly object sendFileLock = new();
        readonly object recievingFileLock = new();

        public event EventHandler<ClientMission> Recieving;
        public event EventHandler<ClientMission> Sending;
        public static void Log(string message) => App.Log(message);
        public MyTcpClient(int port, int maxBufferSize)
        {
            this.port = port;
            this.maxBufferSize = maxBufferSize;
        }
        public Task StartSendFile(ClientMission mission,Stream stream=null)
        {
            lock (sendFileLock)
            {
                stream ??=mission.OpenRead();
                MyTcpClient.Log($"start send file {mission.FileName} to {mission.IPEndPoint} in position {mission.Position}");
                if (mission.Position != 0)
                {
                    stream.Seek(mission.Position, SeekOrigin.Begin);
                }
                mission.CancellationTokenSource= new CancellationTokenSource();
                mission.Type = MissionType.Sending;
                return Task.Run(async () =>
                {
                    using var tcpFileSender = new TcpFileSender(maxBufferSize);
                    using (stream)
                    {
                        var sended = await tcpFileSender.SendFileAsync(stream, new IPEndPoint(mission.IPEndPoint.Address, port), p => OnSending(mission, p),mission.CancellationTokenSource.Token);
                        if (sended)
                        {
                            if (stream.Position < stream.Length)
                            {
                                MyTcpClient.Log($"file {mission.FileName} sended stopped");
                                mission.Type = MissionType.WaitResumeSending;
                            }
                            else
                            {
                                mission.Type = MissionType.SendingCompleted;
                                MyTcpClient.Log($"file {mission.FileName} sended");
                            }
                        }
                        else
                        {
                            MyTcpClient.Log($"file {mission.FileName} sended stopped");
                            mission.Type = MissionType.WaitResumeSending;
                        }
                    }
                });
            }
        }
        public Task Resend(ClientMission mission)
        {
            return StartSendFile(mission);
        }
        void OnSending(ClientMission mission, long position)
        {
            mission.Position = position;
            mission.Type = MissionType.Sending;
            Sending?.Invoke(this, mission);
        }

        public Task StartRecievingFile(ClientMission mission)
        {
            lock (recievingFileLock)
            {
                Stream stream;
                MyTcpClient.Log($"start recieing file({mission.FileName}) in position {mission.Position} from {mission.IPEndPoint} in path{mission.FilePath}");
                if (mission.FilePath != null&&mission.FilePlatformPath!=null)
                {
                    stream = mission.OpenWrite();
                    MyTcpClient.Log($"open created file {mission.FilePath} for save recieing file");
                }
                else
                {
                    var file = FileSaveManager.CreateFile(mission.FileName, mission.FileType);
                    mission.FilePath = file.Path;
                    mission.FilePlatformPath = file.PlatformPath;
                    stream = file.Stream;
                    MyTcpClient.Log($"create file {mission.FilePath} for save recieing file");
                }
                if (mission.Position != 0)
                {
                    stream.Seek(mission.Position, SeekOrigin.Begin);
                }
                mission.CancellationTokenSource = new CancellationTokenSource();
                mission.Type = MissionType.Recieving;
                return Task.Run(async () =>
                {
                    var tcpFileReciever = new TcpFileReciever(maxBufferSize, port);
                    var recieved = await tcpFileReciever.RecievingFile(stream, p => OnRecieving(mission, p), mission.CancellationTokenSource.Token,startPosition:mission.Position);
                    using (stream) { }
                    if (recieved.CompletedNormal)
                    {
                        if (recieved.Readed < mission.FileLength)
                        {
                            MyTcpClient.Log($"file {mission.FileName} recieve stopped");
                            mission.Type = MissionType.WaitResumeRecieving;
                        }
                        else
                        {
                            mission.Type = MissionType.RecievingComleted;
                        }
                    }
                    else
                    {
                        MyTcpClient.Log($"file {mission.FileName} recieve stopped");
                        mission.Type = MissionType.WaitResumeRecieving;
                    }
                });
            }
        }


        public Task ResumeReciving(ClientMission mission)
        {
            return StartRecievingFile(mission);
        }
        void OnRecieving(ClientMission mission,long position)
        {
            mission.Position=position;  
            Recieving?.Invoke(this,mission);
        }
    }
}
