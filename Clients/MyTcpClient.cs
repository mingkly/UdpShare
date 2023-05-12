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
        int port;
        int maxBufferSize;
        object sendFileLock = new object();
        object recievingFileLock = new object();
        FileActions.FilePickers.IFilePicker filePicker;
        IFileSaver fileSaver;

        public event EventHandler<FileResultEventArgs> Recieving;
        public event EventHandler<FileResultEventArgs> Sending;
        public void Log(string message)=>App.Log(message);
        public Dictionary<uint, SendingFile> SendingFiles { get; private set; }
        public Dictionary<uint, RecievingFile> RecievingFiles { get; private set; }
        public MyTcpClient(int port, int maxBufferSize, FileActions.FilePickers.IFilePicker filePicker, IFileSaver fileSaver)
        {
            this.port = port;
            this.maxBufferSize = maxBufferSize;
            SendingFiles = new Dictionary<uint, SendingFile>();
            RecievingFiles = new Dictionary<uint, RecievingFile>();
            this.filePicker = filePicker;
            this.fileSaver = fileSaver;
        }
        public Task StartSendFile(uint fileId, string path, string fileName, IPEndPoint endPoint, long startPostion = 0)
        {
            lock (sendFileLock)
            {
                if (SendingFiles.ContainsKey(fileId))
                {
                    return Task.CompletedTask;
                }
                Log($"start send file {fileName} to {endPoint} in position {startPostion}");
                var stream = filePicker.OpenPickedFile(path);
                if (startPostion != 0)
                {
                    stream.Seek(startPostion, SeekOrigin.Begin);
                }
                var cancellationTokenSource = new CancellationTokenSource();
                var sendingFile = new SendingFile(cancellationTokenSource, fileId, path, fileName, stream.Length, endPoint);
                SendingFiles.TryAdd(fileId,sendingFile );
                OnSendingStart(fileId);
                return Task.Factory.StartNew(async () =>
                {
                    using var tcpFileSender = new TcpFileSender(maxBufferSize);
                    using (stream)
                    {
                        var sended = await tcpFileSender.SendFileAsync(stream, new IPEndPoint(endPoint.Address, port), p => OnSending(fileId, p), cancellationTokenSource.Token);
                        if (sended)
                        {
                            if(stream.Position<stream.Length)
                            {
                                Log($"file {fileName} sended stopped");
                                Sending?.Invoke(this, new FileResultEventArgs(sendingFile, FileResultState.Stop, stream.Position));
                            }
                            else
                            {
                                OnSended(fileId);
                                Log($"file {fileName} sended");
                            }                           
                        }
                        else
                        {
                            Log($"file {fileName} sended stopped");
                            Sending?.Invoke(this, new FileResultEventArgs(sendingFile, FileResultState.Stop, stream.Position));
                        }
                    }
                }, TaskCreationOptions.LongRunning);
            }
        }
        public void StopSending(uint fileId)
        {
            if (SendingFiles.ContainsKey(fileId))
            {
                SendingFiles[fileId].CancellationTokenSource.Cancel();
            }
        }
        public void Resend(uint fileId, long startPosition)
        {
            if (SendingFiles.ContainsKey(fileId))
            {
                var file = SendingFiles[fileId];
                SendingFiles.Remove(fileId);
                StartSendFile(file.FileId, file.FilePath, file.FileName, file.IPEndPoint, startPosition);
            }
        }
        void OnSendingStart(uint fileId)
        {
            Sending?.Invoke(this, new FileResultEventArgs(SendingFiles[fileId], FileResultState.Start));
        }
        void OnSending(uint fileId, long position)
        {
            SendingFiles[fileId].Position = position;
            Sending?.Invoke(this, new FileResultEventArgs(SendingFiles[fileId], FileResultState.Updating,position));
        }
        void OnSended(uint fileId)
        {
            SendingFiles[fileId].Position = SendingFiles[fileId].FileLength;
            Sending?.Invoke(this, new FileResultEventArgs(SendingFiles[fileId], FileResultState.Ending,1));
        }


        public void StartRecievingFile(uint fileId, string fileName, long fileLength, FileType fileType,IPEndPoint senderIp, long startPostion = 0, string savedPath = null)
        {
            lock (recievingFileLock)
            {
                if (RecievingFiles.ContainsKey(fileId))
                {
                    return;
                }
                Stream stream;
                Log($"start recieing file({fileName}) in position {startPostion} from {senderIp} in path{savedPath}");
                if (savedPath != null)
                {
                    stream = fileSaver.OpenCreatedFile(savedPath);
                }
                else
                {
                    var file = fileSaver.Create(fileName, fileLength, fileType);
                    savedPath = file.Path;
                    stream = file.Stream;
                    Log($"create file {savedPath} for save recieing file");
                }
                if (startPostion != 0)
                {
                    stream.Seek(startPostion, SeekOrigin.Begin);
                }
                var cancellationTokenSource = new CancellationTokenSource();
                var recievingFile = new RecievingFile(cancellationTokenSource, fileId, fileName, savedPath, fileLength, fileType,senderIp);
                RecievingFiles.TryAdd(fileId, recievingFile);
                OnRecieveStart(fileId);
                Task.Factory.StartNew(async () =>
                {
                    var tcpFileReciever = new TcpFileReciever(maxBufferSize, port);
                    var recieved = await tcpFileReciever.RecievingFile(stream, p => OnRecieving(fileId, p), cancellationTokenSource.Token);
                    var info = new FileCreateInfo
                    {
                        Path = savedPath,
                        FileType = fileType,
                        Stream = stream,
                    };
                    var position=stream.Position;
                    await fileSaver.SaveAsync(info);
                    if (recieved)
                    {
                        if (position < fileLength)
                        {
                            Log($"file {fileName} recieve stopped");
                            Recieving?.Invoke(this, new FileResultEventArgs(recievingFile, FileResultState.Stop, position));
                        }
                        else
                        {
                            OnRecieved(fileId);
                            Log($"file {fileName} recieved");
                        }                        
                    }
                    else
                    {
                        Log($"file {fileName} recieve stopped");
                        Recieving?.Invoke(this, new FileResultEventArgs(recievingFile, FileResultState.Stop, position));
                    }
                }, TaskCreationOptions.LongRunning);
            }
        }

        public void StopRecieving(uint fileId)
        {
            if (RecievingFiles.ContainsKey(fileId))
            {
                RecievingFiles[fileId].CancellationTokenSource?.Cancel();
            }
        }
        public void ResumeReciving(uint fileId, long startposition)
        {
            if (RecievingFiles.ContainsKey(fileId))
            {
                var file = RecievingFiles[fileId];
                RecievingFiles.Remove(fileId);
                StartRecievingFile(fileId, file.FileName, file.FileLength, file.FileType,file.SenderIp, startposition, file.SavedPath);
            }
        }
        void OnRecieveStart(uint fileId)
        {
            Recieving?.Invoke(this, new FileResultEventArgs(RecievingFiles[fileId], FileResultState.Start, 0));
        }
        void OnRecieving(uint fileId, long position)
        {
            RecievingFiles[fileId].Position= position;
            Recieving?.Invoke(this, new FileResultEventArgs(RecievingFiles[fileId], FileResultState.Updating, position));
        }
        void OnRecieved(uint fileId)
        {
            RecievingFiles[fileId].Position = RecievingFiles[fileId].FileLength;
            Recieving?.Invoke(this, new FileResultEventArgs(RecievingFiles[fileId], FileResultState.Ending, 1));
        }

        public void SaveHistoryToDataStore(IDataStore dataStore)
        {
            dataStore.Save(nameof(SendingFiles),JsonSerializer.Serialize( SendingFiles));
            dataStore.Save(nameof(RecievingFiles), JsonSerializer.Serialize(RecievingFiles));
        }
        public void LoadHistoryFromDataStore(IDataStore dataStore)
        {
            try
            {
                var rjson = dataStore.Get<string>(nameof(RecievingFiles));
                if (rjson != null)
                {
                    RecievingFiles = JsonSerializer.Deserialize<Dictionary<uint, RecievingFile>>(rjson) ?? RecievingFiles;
                }
                var sjson = dataStore.Get<string>(nameof(SendingFiles));
                if (sjson != null)
                {
                    SendingFiles = JsonSerializer.Deserialize<Dictionary<uint, SendingFile>>(sjson) ?? SendingFiles;
                }
            }
            catch { }
        }
    }
}
