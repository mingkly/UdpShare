
using MKFilePicker;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using UdpQuickShare.FileActions;
using UdpQuickShare.FileActions.FileSavers;
using UdpQuickShare.Protocols;
using UdpQuickShare.Services;


namespace UdpQuickShare.Clients
{
    public class ShareClient
    {
        readonly MyUdpClient udpClient;
        readonly MyTcpClient tcpClient;
        readonly IPEndPoint broadcast;
        readonly IDataStore dataStore;
        readonly int maxBufferSize;
        bool isWaitingUdp;



        public event EventHandler<ClientMission> SendingError;
        public event EventHandler<DeviceNotFoundEventArgs> DeviceNotFound;
        public event EventHandler<DeviceFoundEventArgs> OnDeviceFound;
        public event EventHandler<IPEndPoint> CurrentIpChanged;
        public event EventHandler<bool> ExposedChanged;

        public event EventHandler<ClientMission> OnMissionHandled;
        public IList<ClientMission> Missions { get; private set; }
        TaskCompletionSource<bool> CheckForConnectionTask;
        public MyTcpClient TcpClient => tcpClient;
        public MyUdpClient UdpClient => udpClient;
        bool exposed;
        public bool Exposed
        {
            get => exposed;
            private set
            {
                if (exposed != value)
                {
                    exposed = value;
                    ExposedChanged?.Invoke(this, exposed);
                }
            }
        }
        public static void Log(string message) => App.Log(message);
        public ShareClient(IEncoder encoder, IDecoder decoder, IDataStore dataStore, int port, int tcpPort, int maxBufferSize, IList<ClientMission> missions)
        {
            udpClient = new MyUdpClient(encoder, decoder, port, maxBufferSize);
            tcpClient = new MyTcpClient(tcpPort, maxBufferSize);
            broadcast = new IPEndPoint(IPAddress.Broadcast, port);
            this.maxBufferSize = maxBufferSize;
            this.dataStore = dataStore;
            udpClient.CurrentIpChanged += UdpClient_CurrentIpChanged;

            tcpClient.Recieving += TcpClient_Recieving;
            tcpClient.Sending += TcpClient_Sending;
            Missions = missions;
        }

        private void TcpClient_Sending(object sender, ClientMission e)
        {
            MissionUpdated(e);
        }

        private void TcpClient_Recieving(object sender, ClientMission e)
        {
            MissionUpdated(e);
        }

        private void UdpClient_CurrentIpChanged(object sender, EventArgs e)
        {
            CurrentIpChanged?.Invoke(this, udpClient.CurrentIp);
        }
        public async Task SetUp()
        {
            await udpClient.GetOwnIp();
            await UdpClient.SendAsync(new CommandSegement(App.DeviceName, CommandType.FoundDevice), broadcast);
        }
        public Task FindDevices()
        {
            return UdpClient.SendAsync(new CommandSegement(App.DeviceName, CommandType.FoundDevice), broadcast);
        }
        public void ExposeThisDevice()
        {
            if (Exposed)
            {
                return;
            }
            Exposed = true;
            udpClient.StartRecieving();
            udpClient.OnRecieved += UdpClient_OnRecieved;
        }
        public void DisableThisDevice()
        {
            if (!Exposed)
            {
                return;
            }
            Exposed = false;
            udpClient.StopRecieving();
            udpClient.OnRecieved -= UdpClient_OnRecieved;
        }

        private async void UdpClient_OnRecieved(object sender, UdpRecievedEventArgs e)
        {
            try
            {
                if (e.Segement is HeadSegement head)
                {
                    ShareClient.Log($"recieving file head ,reply and start reciving file");
                    var replySegement = new ReplySegement(e.Segement.FileId);
                    await udpClient.SendAsync(replySegement, e.IP);
                    var mission = new ClientMission(MissionType.WaitRecieving,
                        head.FileId,
                        head.FileName,
                        head.FileType,
                        head.FileLength,
                        0,
                        e.IP);
                    if (head.FileLength > maxBufferSize)
                    {
                        MissionUpdated(mission);
                        await tcpClient.StartRecievingFile(mission);
                        MissionUpdated(mission);
                    }
                    else
                    {
                        MissionUpdated(mission);
                        //InvokeStartRecieving(head.FileId);
                        isWaitingUdp = true;
                    }
                }
                else if (e.Segement is CommandSegement command)
                {
                    if (command.Command == CommandType.FoundDevice)
                    {
                        ShareClient.Log($"recieve other found device command {command.Value}({e.IP}) and reply");
                        await udpClient.SendAsync(new CommandSegement(App.DeviceName, CommandType.DeviceReply), e.IP);
                        OnDeviceFound?.Invoke(this, new DeviceFoundEventArgs(e.IP, command.Value));
                    }
                    else if (command.Command == CommandType.DeviceReply)
                    {
                        ShareClient.Log($"recieve other found device reply command {command.Value}({e.IP})");
                        OnDeviceFound?.Invoke(this, new DeviceFoundEventArgs(e.IP, command.Value));
                    }
                    else if (command.Command == CommandType.RecieverAskForResending)
                    {
                        if (TryGetMission(command.FileId, out var mission))
                        {
                            if (!mission.FileExist())
                            {
                                return;
                            }
                            var position = long.Parse(command.Value);
                            ShareClient.Log($"recieve RecieverAskForResending in position {position},reply and start resend");
                            await udpClient.SendAsync(new CommandSegement(command.FileId, "", CommandType.SenderReadyForResending), e.IP);
                            mission.Position = position;
                            await tcpClient.Resend(mission);
                            MissionUpdated(mission);
                        }
                    }
                    else if (command.Command == CommandType.SenderReadyForResending)
                    {
                        if (TryGetMission(command.FileId, out var mission))
                        {
                            await tcpClient.ResumeReciving(mission);
                            MissionUpdated(mission);
                        }
                    }
                    else if (command.Command == CommandType.SenderAskForResending)
                    {
                        if (TryGetMission(command.FileId, out var mission))
                        {
                            long position = mission.FileExist() ? mission.Position : 0;
                            mission.Position = position;
                            await udpClient.SendAsync(new CommandSegement(command.FileId, position.ToString(), CommandType.RecieverReadyForRecieving), e.IP);
                            ShareClient.Log($"recieve SenderAskForResending,start recieving in position {mission.Position} and reply");
                            await tcpClient.ResumeReciving(mission);
                            MissionUpdated(mission);
                        }
                    }
                    else if (command.Command == CommandType.RecieverReadyForRecieving)
                    {
                        if (TryGetMission(command.FileId, out var mission))
                        {
                            var position = long.Parse(command.Value);
                            mission.Position = position;
                            ShareClient.Log($"recieve RecieverReadyForRecieving in position{position},start resending");
                            await tcpClient.Resend(mission);
                            MissionUpdated(mission);
                        }
                    }
                    else if (command.Command == CommandType.StopRecieving)
                    {
                        var fileId = command.FileId;
                        ShareClient.Log($"recieve RecieverStopRecieving ,stop sending");
                        StopSending(fileId);
                    }
                    else if (command.Command == CommandType.CheckForConnection)
                    {
                        ShareClient.Log($"recieve CheckForConnection and reply");
                        await udpClient.SendAsync(new CommandSegement("", CommandType.ConnectionNormal), e.IP);
                    }
                    else if (command.Command == CommandType.ConnectionNormal)
                    {
                        ShareClient.Log($"recieve CheckForConnection reply and setresult");
                        CheckForConnectionTask?.TrySetResult(true);
                    }
                }
                else if (isWaitingUdp && e.Segement is PartByteSegement byteSegement)
                {

                    if (!TryGetMission(e.Segement.FileId, out var mission))
                    {
                        return;
                    }
                    ShareClient.Log($"recieve file {mission.FileId}({mission.FileName}) from udp");
                    var file = FileSaveManager.CreateFile(mission.FileName, mission.FileType);
                    file.Stream.Write(byteSegement.TotalData, byteSegement.Offset, byteSegement.Count);
                    mission.FilePath = file.Path;
                    mission.FilePlatformPath = file.PlatformPath;
                    mission.Position = mission.FileLength;
                    mission.Type = MissionType.RecievingComleted;
                    if (mission.FileType == FileType.Text)
                    {
                        var textValue = Encoding.UTF8.GetString(byteSegement.TotalData, byteSegement.Offset, byteSegement.Count);
                        mission.Text = textValue;
                    }
                    using (file.Stream) { }
                    MissionUpdated(mission);

                    isWaitingUdp = false;
                }
            }
            catch { }

        }
        async Task<bool> ConnectAsync(ClientMission mission)
        {
            if (mission.IPEndPoint == null)
            {
                return false;
            }
            var head = new HeadSegement(mission.FileName, mission.FileId, mission.FileLength, mission.FileType);
            ShareClient.Log($"send file head and waiting for reply");
            var (Sender, Segement) = await udpClient.SendForResultAsync(head, mission.IPEndPoint, 3000);
            if (Segement is not ReplySegement)
            {
                return false;
            }
            return true;
        }
        public async Task HandleMission(ClientMission mission)
        {
            try
            {
                if (!(await ConnectAsync(mission)))
                {
                    ShareClient.Log($"reply not found");
                    DeviceNotFound?.Invoke(this, new DeviceNotFoundEventArgs(mission.IPEndPoint));
                    return;
                }
                if (mission.Type == MissionType.WaitSending)
                {
                    var stream = mission.OpenRead();
                    if (mission.FileLength > maxBufferSize)
                    {
                        ShareClient.Log($"send bog file use tcp");
                        MissionUpdated(mission);
                        await tcpClient.StartSendFile(mission, stream);
                        MissionUpdated(mission);
                    }
                    else
                    {
                        await SendUseUdp(stream, mission);
                        MissionUpdated(mission);
                    }
                }
            }
            catch (Exception ex)
            {
                SendingError?.Invoke(this, mission);
            }

        }
        async Task SendUseUdp(Stream stream, ClientMission mission)
        {
            ShareClient.Log($"send small file use udp");
            using (stream)
            {
                var body = new StreamBodySegement(mission.FileId, 0, stream, 0, (uint)stream.Length);
                await udpClient.SendAsync(body, mission.IPEndPoint);
                mission.Type = MissionType.SendingCompleted;
                mission.Position = stream.Length;
            }
        }


        public void StopSending(uint fileId)
        {
            try
            {
                if (TryGetMission(fileId, out var mission))
                {
                    mission.CancellationTokenSource?.Cancel();
                    mission.Type = MissionType.WaitResumeSending;
                    ShareClient.Log($"stop sending file {fileId} to {mission.IPEndPoint}");
                    //MissionUpdated(mission);
                }
            }
            catch { }
        }
        public Task ResumeSending(uint fileId)
        {
            if (TryGetMission(fileId, out var mission))
            {
                if (!mission.FileExist())
                {
                    SendingError?.Invoke(this, mission);
                }
                ShareClient.Log($"resume sending file {fileId} to {mission.IPEndPoint} ");
                return udpClient.SendAsync(new CommandSegement(fileId, "", CommandType.SenderAskForResending), mission.IPEndPoint);
            }
            return Task.CompletedTask;
        }
        public Task StopRecieving(uint fileId)
        {
            try
            {
                if (TryGetMission(fileId, out var mission))
                {
                    mission.CancellationTokenSource?.Cancel();
                    mission.Type = MissionType.WaitResumeRecieving;
                    ShareClient.Log($"stop reciving file {fileId} in position {mission.Position} from {mission.IPEndPoint} ");
                    return udpClient.SendAsync(new CommandSegement(fileId, "", CommandType.StopRecieving), mission.IPEndPoint);
                }
            }
            catch { }
            return Task.CompletedTask;
        }
        public Task ResumeRecieving(uint fileId)
        {
            if (TryGetMission(fileId, out var mission))
            {
                long position = mission.FileExist() ? mission.Position : 0;
                mission.Position = position;
                ShareClient.Log($"resume reciving file {fileId} in position {mission.Position} from {mission.IPEndPoint} ");
                return udpClient.SendAsync(new CommandSegement(fileId, mission.Position.ToString(), CommandType.RecieverAskForResending), mission.IPEndPoint);
            }
            return Task.CompletedTask;
        }
        public async Task<bool> CheckForConnection(IPEndPoint ip, int timeout = 1000)
        {
            if (CheckForConnectionTask != null)
            {
                return false;
            }
            ShareClient.Log($"check for connection to {ip}");
            CheckForConnectionTask = new TaskCompletionSource<bool>();
            await udpClient.SendAsync(new CommandSegement("", CommandType.CheckForConnection), ip);
            var t2 = Task.Delay(timeout);
            await Task.WhenAny(CheckForConnectionTask.Task, t2);
            if (CheckForConnectionTask.Task.IsCompleted)
            {
                ShareClient.Log($"connection to {ip} normal");
                CheckForConnectionTask = null;
                return true;
            }
            ShareClient.Log($"connection to {ip} failed");
            CheckForConnectionTask = null;
            return false;
        }

        void MissionUpdated(ClientMission mission, [CallerMemberName] string caller = "")
        {
            OnMissionHandled?.Invoke(this, mission);
        }
        ClientMission GetMission(uint fileId)
        {
            return Missions.FirstOrDefault(m => m.FileId == fileId);
        }
        bool TryGetMission(uint fileId, out ClientMission mission)
        {
            mission = GetMission(fileId);
            return mission != null;
        }

    }
}
