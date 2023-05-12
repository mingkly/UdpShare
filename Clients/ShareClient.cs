
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using UdpQuickShare.FileActions;
using UdpQuickShare.FileActions.FilePickers;
using UdpQuickShare.FileActions.FileSavers;
using UdpQuickShare.Protocols;
using UdpQuickShare.Services;


namespace UdpQuickShare.Clients
{
    public class ShareClient
    {
        MyUdpClient udpClient;
        MyTcpClient tcpClient;
        IPEndPoint broadcast;
        IFileSaver fileSaver;
        IDataStore dataStore;
        int maxBufferSize;
        bool isWaitingUdp;
        int tcpPort;
        Dictionary<uint, HeadSegement> recievingHeads;
        Dictionary<uint, HeadSegement> sendingHeads;
        public event EventHandler<FileResultEventArgs> Recieving;
        public event EventHandler<FileResultEventArgs> Sending;
        public event EventHandler<PickFileResult> SendingError;
        public event EventHandler<DeviceNotFoundEventArgs> DeviceNotFound;
        public event EventHandler<DeviceFoundEventArgs> OnDeviceFound;
        public event EventHandler<IPEndPoint> CurrentIpChanged;
        public event EventHandler<bool> ExposedChanged;
        public Dictionary<uint,UdpFileResult> UdpFiles { get; private set; }
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
        public void Log(string message) => App.Log(message);
        public ShareClient(IEncoder encoder, IDecoder decoder, IFileSaver fileSaver, IDataStore dataStore, int port, int tcpPort, int maxBufferSize)
        {
            udpClient = new MyUdpClient(encoder, decoder, port, maxBufferSize);
            this.tcpPort = tcpPort;
            tcpClient = new MyTcpClient(tcpPort, maxBufferSize, ServiceFactory.CreateFilePicker(), fileSaver);
            broadcast = new IPEndPoint(IPAddress.Broadcast, port);
            this.fileSaver = fileSaver;
            this.maxBufferSize = maxBufferSize;
            this.dataStore = dataStore;
            tcpClient.LoadHistoryFromDataStore(dataStore);
            recievingHeads = new Dictionary<uint, HeadSegement>();
            sendingHeads = new Dictionary<uint, HeadSegement>();
            udpClient.CurrentIpChanged += UdpClient_CurrentIpChanged;
            tcpClient.Recieving += TcpClient_Recieving;
            tcpClient.Sending += TcpClient_Sending;
            LoadUdpFiles();
        }

        private void TcpClient_Sending(object sender, FileResultEventArgs e)
        {
            Sending?.Invoke(this, e);
            if (e.State == FileResultState.Start || e.State == FileResultState.Stop
                || e.State == FileResultState.Ending)
            {
                tcpClient.SaveHistoryToDataStore(dataStore);
            }
        }

        private void TcpClient_Recieving(object sender, FileResultEventArgs e)
        {
            if (e.State == FileResultState.Start || e.State == FileResultState.Stop
                || e.State == FileResultState.Ending)
            {
                tcpClient.SaveHistoryToDataStore(dataStore);
                Debug.WriteLine(TcpClient.RecievingFiles.First().Value.Position);
            }
            Recieving?.Invoke(this, e);
            if (e.State == FileResultState.Ending)
            {
                ExposeThisDevice();
            }

        }

        private void UdpClient_CurrentIpChanged(object sender, EventArgs e)
        {
            CurrentIpChanged?.Invoke(this, udpClient.CurrentIp);
        }
        public void SetUp()
        {
            Task.Run(async () =>
            {
                await udpClient.GetOwnIp();
                await UdpClient.SendAsync(new CommandSegement(App.DeviceName, CommandType.FoundDevice), broadcast);
            });
        }
        public void FindDevices()
        {
            Task.Run(() => UdpClient.SendAsync(new CommandSegement(App.DeviceName, CommandType.FoundDevice), broadcast));
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
                    if (!recievingHeads.ContainsKey(head.FileId))
                    {
                        recievingHeads.Add(head.FileId, head);
                    }
                    Log($"recieving file head ,reply and start reciving file");
                    var replySegement = new ReplySegement(e.Segement.FileId);
                    await udpClient.SendAsync(replySegement, e.IP);
                    if (head.FileLength > maxBufferSize)
                    {
                        tcpClient.StartRecievingFile(head.FileId, head.FileName, head.FileLength, head.FileType, e.IP);
                    }
                    else
                    {
                        //InvokeStartRecieving(head.FileId);
                        isWaitingUdp = true;
                    }
                }
                else if (e.Segement is CommandSegement command)
                {
                    if (command.Command == CommandType.FoundDevice)
                    {
                        Log($"recieve other found device command {command.Value}({e.IP}) and reply");
                        await udpClient.SendAsync(new CommandSegement(App.DeviceName, CommandType.DeviceReply), e.IP);
                        OnDeviceFound?.Invoke(this, new DeviceFoundEventArgs(e.IP, command.Value));
                    }
                    else if (command.Command == CommandType.DeviceReply)
                    {
                        Log($"recieve other found device reply command {command.Value}({e.IP})");
                        OnDeviceFound?.Invoke(this, new DeviceFoundEventArgs(e.IP, command.Value));
                    }
                    else if (command.Command == CommandType.RecieverAskForResending)
                    {
                        var position = uint.Parse(command.Value);
                        Log($"recieve RecieverAskForResending in position {position},reply and start resend");
                        await udpClient.SendAsync(new CommandSegement(command.FileId, "", CommandType.SenderReadyForResending), e.IP);
                        tcpClient.Resend(command.FileId, position);
                    }
                    else if (command.Command == CommandType.SenderReadyForResending)
                    {
                        var fileId = command.FileId;
                        var position = tcpClient.RecievingFiles[fileId].Position;
                        Log($"recieve SenderReadyForResending,start recieving in position {position}");
                        tcpClient.ResumeReciving(fileId, position);
                    }
                    else if (command.Command == CommandType.SenderAskForResending)
                    {
                        var fileId = command.FileId;
                        var position = tcpClient.RecievingFiles[fileId].Position;
                        Log($"recieve SenderAskForResending,start recieving in position {position} and reply");
                        tcpClient.ResumeReciving(fileId, position);
                        await udpClient.SendAsync(new CommandSegement(command.FileId, position.ToString(), CommandType.RecieverReadyForRecieving), e.IP);
                    }
                    else if (command.Command == CommandType.RecieverReadyForRecieving)
                    {
                        var fileId = command.FileId;
                        var position = uint.Parse(command.Value);
                        Log($"recieve RecieverReadyForRecieving in position{position},start resending");
                        tcpClient.Resend(fileId, position);
                    }
                    else if (command.Command == CommandType.StopRecieving)
                    {
                        var fileId = command.FileId;
                        Log($"recieve RecieverStopRecieving ,stop sending");
                        tcpClient.StopSending(fileId);
                    }
                    else if (command.Command == CommandType.CheckForConnection)
                    {
                        Log($"recieve CheckForConnection and reply");
                        await udpClient.SendAsync(new CommandSegement("", CommandType.ConnectionNormal), e.IP);
                    }
                    else if (command.Command == CommandType.ConnectionNormal)
                    {
                        Log($"recieve CheckForConnection reply and setresult");
                        CheckForConnectionTask?.TrySetResult(true);
                    }
                }
                else if (isWaitingUdp && e.Segement is PartByteSegement byteSegement)
                {
                    var fileHead = recievingHeads[byteSegement.FileId];
                    Log($"recieve file {fileHead.FileId}({fileHead.FileName}) from udp");
                    var file = fileSaver.Create(fileHead.FileName, fileHead.FileLength, fileHead.FileType);
                    file.Stream.Write(byteSegement.TotalData, byteSegement.Offset, byteSegement.Count);
                    string textValue = null;
                    if (fileHead.FileType == FileType.Text)
                    {
                        textValue = Encoding.UTF8.GetString(byteSegement.TotalData, byteSegement.Offset, byteSegement.Count);
                        InvokeEndRecievingText(fileHead.FileId, textValue);
                    }
                    var info = new FileCreateInfo
                    {
                        Path = file.Path,
                        FileType = file.FileType,
                        Stream = file.Stream,
                    };
                    await fileSaver.SaveAsync(info);
                    if (fileHead.FileType != FileType.Text)
                    {
                        InvokeEndRecieving(fileHead.FileId, info.Path);
                    }
                    AddRecievingUdpFile(new UdpFileResult
                    {
                        FileId = fileHead.FileId,
                        Path = info.Path,
                        FileLength = fileHead.FileLength,
                        IsSending = false,
                        FileName = fileHead.FileName,
                        TextValue = textValue
                    });
                    isWaitingUdp = false;
                }
            }
            catch { }

        }
        public async Task SendFileAsync(PickFileResult pickFileResult, FileType fileType, IEnumerable<IPEndPoint> targets)
        {
            try
            {
                uint id = (uint)pickFileResult.GetHashCode();

                if (!targets.Any())
                {
                    Log($"send file targets empty");
                    DeviceNotFound?.Invoke(this, new DeviceNotFoundEventArgs(null));
                    return;
                }
                Log($"start send file {id}({pickFileResult.Name}) to {string.Join(",", targets)}");
                if (!sendingHeads.ContainsKey(id))
                {
                    var head = new HeadSegement(pickFileResult.Name, id, (uint)pickFileResult.Length, fileType);
                    sendingHeads.Add(id, head);

                    foreach (var client in targets)
                    {
                        Log($"send file head and waiting for reply");
                        var reply = await udpClient.SendForResultAsync(head, client, 3000);
                        if (reply.Segement is not ReplySegement)
                        {
                            Log($"reply not found");
                            DeviceNotFound?.Invoke(this, new DeviceNotFoundEventArgs(client));
                            continue;
                        }
                        if (head.FileLength > maxBufferSize)
                        {
                            Log($"send bog file use tcp");
                            _=tcpClient.StartSendFile(id, pickFileResult.Uri, pickFileResult.Name, client);
                        }
                        else
                        {
                            Log($"send small file use udp");
                            //InvokeStartSending(id);
                            pickFileResult.Stream ??= ServiceFactory.CreateFilePicker().OpenPickedFile(pickFileResult.Uri);
                            using (pickFileResult.Stream)
                            {
                                var body = new StreamBodySegement(id, 0, pickFileResult.Stream, 0, (uint)pickFileResult.Length);
                                await udpClient.SendAsync(body, client);
                                string textValue = null;
                                try
                                {
                                    if (head.FileType == FileType.Text)
                                    {
                                        pickFileResult.Stream.Position = 0;
                                        using (var sr = new StreamReader(pickFileResult.Stream))
                                        {
                                            textValue = sr.ReadToEnd();
                                        }
                                    }
                                }
                                catch { }
                                AddSendingUdpFile(new UdpFileResult
                                {
                                    FileId = id,
                                    FileName = head.FileName,
                                    TextValue = textValue,
                                    FileLength = head.FileLength,
                                    IsSending = true,
                                    Path = pickFileResult.Uri,
                                });
                                if (textValue != null)
                                {
                                    InvokeEndSendingText(id, textValue);
                                }
                                else
                                {
                                    InvokeEndSending(id);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                SendingError?.Invoke(this, pickFileResult);
            }

        }

        public async Task SendMultiFileAsync(IEnumerable<(PickFileResult pickFileResult, FileType fileType)> files, IPEndPoint target)
        {
            try
            {
                Log($"start send multi files");
                foreach (var file in files)
                {
                    uint id = (uint)file.pickFileResult.GetHashCode();
                    if (target == null)
                    {
                        Log($"send file targets empty");
                        DeviceNotFound?.Invoke(this, new DeviceNotFoundEventArgs(null));
                        return;
                    }
                    Log($"start send file {id}({file.pickFileResult.Name}) to {target}");
                    if (!sendingHeads.ContainsKey(id))
                    {
                        var head = new HeadSegement(file.pickFileResult.Name, id, (uint)file.pickFileResult.Length, file.fileType);
                        sendingHeads.TryAdd(id, head);
                        var client = target;
                        Log($"send file head and waiting for reply");
                        var reply = await udpClient.SendForResultAsync(head, client, 3000);
                        if (reply.Segement is not ReplySegement)
                        {
                            Log($"reply not found");
                            DeviceNotFound?.Invoke(this, new DeviceNotFoundEventArgs(client));
                            continue;
                        }
                        if (head.FileLength > maxBufferSize)
                        {
                            Log($"send bog file use tcp");
                            await tcpClient.StartSendFile(id, file.pickFileResult.Uri, file.pickFileResult.Name, client);
                        }
                        else
                        {
                            Log($"send small file use udp");
                            //InvokeStartSending(id);
                            file.pickFileResult.Stream ??= ServiceFactory.CreateFilePicker().OpenPickedFile(file.pickFileResult.Uri);
                            using (file.pickFileResult.Stream)
                            {
                                var body = new StreamBodySegement(id, 0, file.pickFileResult.Stream, 0, (uint)file.pickFileResult.Length);
                                await udpClient.SendAsync(body, client);
                                string textValue = null;
                                try
                                {
                                    if (head.FileType == FileType.Text)
                                    {
                                        file.pickFileResult.Stream.Position = 0;
                                        using (var sr = new StreamReader(file.pickFileResult.Stream))
                                        {
                                            textValue = sr.ReadToEnd();
                                        }
                                    }
                                }
                                catch { }
                                AddSendingUdpFile(new UdpFileResult
                                {
                                    FileId = id,
                                    FileName = head.FileName,
                                    TextValue = textValue,
                                    FileLength = head.FileLength,
                                    IsSending = true,
                                    Path = file.pickFileResult.Uri,
                                });
                                if(textValue!= null )
                                {
                                    InvokeEndSendingText(id,textValue);
                                }
                                else
                                {
                                    InvokeEndSending(id);
                                }                              
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                SendingError?.Invoke(this, files.FirstOrDefault().pickFileResult);
            }

        }

        public void StopSending(uint fileId)
        {
            try
            {
                var sendingFile = tcpClient.SendingFiles[fileId];
                Log($"stop sending file {fileId} to {sendingFile.IPEndPoint}");
                tcpClient.StopSending(fileId);
            }
            catch { }

        }
        public void ResumeSending(uint fileId)
        {
            Task.Run(async () =>
            {
                var recieverIp = tcpClient.SendingFiles[fileId].IPEndPoint;
                Log($"resume sending file {fileId} to {recieverIp} ");
                await udpClient.SendAsync(new CommandSegement(fileId, "", CommandType.SenderAskForResending), recieverIp);
            });
        }
        public void StopRecieving(uint fileId)
        {

            try
            {
                tcpClient.StopRecieving(fileId);
                var recievingFile = tcpClient.RecievingFiles[fileId];
                Log($"stop reciving file {fileId} in position {recievingFile.Position} from {recievingFile.SenderIp} ");
                ExposeThisDevice();
                Task.Run(async () =>
                {

                    await udpClient.SendAsync(new CommandSegement(fileId, "", CommandType.StopRecieving), recievingFile.SenderIp);
                });
            }
            catch { }

        }
        public void ResumeRecieving(uint fileId)
        {
            Task.Run(async () =>
            {
                var position = tcpClient.RecievingFiles[fileId].Position;
                var senderIp = tcpClient.RecievingFiles[fileId].SenderIp;
                Log($"resume reciving file {fileId} in position {position} from {senderIp} ");
                await udpClient.SendAsync(new CommandSegement(fileId, position.ToString(), CommandType.RecieverAskForResending), senderIp);
            });
        }
        public async Task<bool> CheckForConnection(IPEndPoint ip, int timeout = 1000)
        {
            Log($"check for connection to {ip}");
            CheckForConnectionTask = new TaskCompletionSource<bool>();
            await udpClient.SendAsync(new CommandSegement("", CommandType.CheckForConnection), ip);
            var t2 = Task.Delay(timeout);
            await Task.WhenAny(CheckForConnectionTask.Task, t2);
            if (CheckForConnectionTask.Task.IsCompleted)
            {
                Log($"connection to {ip} normal");
                CheckForConnectionTask = null;
                return true;
            }
            Log($"connection to {ip} failed");
            CheckForConnectionTask = null;
            return false;
        }
        void InvokeStartSending(uint fileId)
        {
            Sending?.Invoke(this, new FileResultEventArgs(sendingHeads[fileId], FileResultState.Start));
        }
        void InvokeEndSending(uint fileId)
        {
            Sending?.Invoke(this, new FileResultEventArgs(sendingHeads[fileId], FileResultState.Ending));
        }
        void InvokeEndSendingText(uint fileId, string text)
        {
            Sending?.Invoke(this, new FileResultEventArgs(sendingHeads[fileId], FileResultState.Ending)
            {
                TextValue = text
            });
        }
        void InvokeStartRecieving(uint fileId)
        {
            Recieving?.Invoke(this, new FileResultEventArgs(recievingHeads[fileId], FileResultState.Start));
        }
        void InvokeEndRecieving(uint fileId, string path)
        {
            Recieving?.Invoke(this, new FileResultEventArgs(recievingHeads[fileId], path, FileResultState.Ending));
        }
        void InvokeEndRecievingText(uint fileId, string text)
        {
            Recieving?.Invoke(this, new FileResultEventArgs(recievingHeads[fileId], FileResultState.Ending)
            {
                TextValue = text
            });
        }
        void AddSendingUdpFile(UdpFileResult udpFileResult)
        {
            UdpFiles.TryAdd(udpFileResult.FileId, udpFileResult);
            dataStore.Save("UdpFiles", JsonSerializer.Serialize(UdpFiles));
        }
        void AddRecievingUdpFile(UdpFileResult udpFileResult)
        {
            UdpFiles.TryAdd(udpFileResult.FileId, udpFileResult);
            dataStore.Save("UdpFiles", JsonSerializer.Serialize(UdpFiles));
        }
        public void SaveUdpFiles()
        {
            dataStore.Save("UdpFiles", JsonSerializer.Serialize(UdpFiles));
        }
        public void LoadUdpFiles()
        {
            try
            {
                var json = dataStore.Get<string>("UdpFiles");
                if (json != null)
                {
                    UdpFiles = JsonSerializer.Deserialize<Dictionary<uint, UdpFileResult>>(json) ?? new Dictionary<uint, UdpFileResult>();
                }
                else
                {
                    UdpFiles = new Dictionary<uint, UdpFileResult>();
                }
            }
            catch
            {
                UdpFiles = new Dictionary<uint, UdpFileResult>();
            }         
        }
    }
}
