
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using UdpQuickShare.Clients;
using UdpQuickShare.FileActions;
using UdpQuickShare.FileActions.FileSavers;
using UdpQuickShare.Pages;
using UdpQuickShare.Protocols;
using UdpQuickShare.Services;
using UdpQuickShare.ViewModels;

namespace UdpQuickShare;

public partial class App : Application
{
    public static App Instance { get; private set; }
    public ShareClient Client { get; private set; }
    public static ObservableCollection<string> Messages { get; private set; }
    
    public ObservableCollection<FileItem> Files { get; private set; }
    public ObservableCollection<FileItem> SendingFiles { get; private set; }
    public static IFileSaver FileSaver { get; private set; }
    public static IDataStore DataStore { get; private set; }
    public  Dictionary<uint, string> PickedFiles { get; private set; }
    public ObservableCollection<DeviceModel> Devices { get; private set; }
    bool sendingOrRecieving;
    public bool SendingOrRecieving
    {
        get => sendingOrRecieving;
        set
        {
            if (sendingOrRecieving != value)
            {
                sendingOrRecieving = value;
                SendingOrRecievingChanged?.Invoke(this, sendingOrRecieving);
            }
        }
    }

    public event EventHandler SendingDeviceChanged;
    public event EventHandler<bool> SendingOrRecievingChanged;
    public static readonly int BufferSize = 1024;
    public static readonly int UdpPort = 4321;
    public static readonly int TcpPort = 4322;
    public static string DeviceName => DeviceInfo.Name;
    public  bool AutoOpen { get; set; } =false;
    public bool opened;
    public App()
    {
        InitializeComponent();
        MainPage = new AppShell();
        Messages = new ObservableCollection<string>();
        DataStore = new PreferenceDataStore();
        FileSaver = ServiceFactory.CreateFileSaver();
        var encoder = new DefaultEncoder();
        var decoder = new DefaultDecoder();
        Client = new ShareClient(encoder, decoder, FileSaver, DataStore, UdpPort, TcpPort, BufferSize);
        LoadFileFromClient();
        LoadDevice();
        Client.Recieving += Client_Recieving;
        Client.Sending += Client_Sending;
        Client.OnDeviceFound += Client_OnDeviceFound;
        Client.DeviceNotFound += Client_DeviceNotFound;
        Instance = this;
        Client.SetUp();
        Client.ExposeThisDevice();
    }
    protected override void OnResume()
    {
        base.OnResume();
        opened = false;
        
    }

    #region Messages
    static void MessagesAdd(string message)
    {
        try
        {
            MainThread.BeginInvokeOnMainThread(() => Messages.Insert(0,message));
        }
        catch(Exception ex)
        {
            //Messages.Add(message);
            //Log(ex);
            //Log(message);
        }     
    }
    public static void Log(string message)
    {
        // MessagesAdd(message);
        //Debug.WriteLine(message);
    }
    public static void Log(object value)
    {
        //Debug.WriteLine(value);
    }
    #endregion
    #region Devices
    void LoadDevice()
    {
        Devices = DeserilizeDevice(DataStore.Get<string>(nameof(Devices))) ?? new ObservableCollection<DeviceModel>();
        foreach(var device in Devices)
        {
            Log($"{device.Name}-{device.SendThis}");
        }
    }
    public void AddOrUpdateDevice(string deviceName, IPEndPoint ip, bool sendThis)
    {
        var existed = Devices.FirstOrDefault(x => x.Ip.Equals(ip));
        var hasSendDevice = Devices.Count(x => x.SendThis) >=1;
        if (existed == null)
        {
            Devices.Add(new DeviceModel(this)
            {
                Ip = ip,
                Name = deviceName,
                SendThis = !hasSendDevice,
            });
        }
        else
        {
            existed.Name = deviceName;
            existed.SendThis = sendThis&&!hasSendDevice;
        }
        DataStore.Save(nameof(Devices), SerilizeDevice(Devices));
        SendingDeviceChanged?.Invoke(Devices, EventArgs.Empty);
    }
    public bool DeleteDevice(IPEndPoint ip)
    {
        var existed = Devices.FirstOrDefault(x => x.Ip.Equals(ip));
        if (existed != null)
        {
            Devices.Remove(existed);
            DataStore.Save(nameof(Devices), SerilizeDevice(Devices));
            SendingDeviceChanged?.Invoke(Devices, EventArgs.Empty);
            Log($"Device {existed.Name}({existed.Ip}) deleted success");
            return true;
        }
        return false;
    }
    internal void ChooseDevice(IPEndPoint ip)
    {
        foreach (var m in Devices)
        {
            m.SendThis = false;
        }
        var deviceModel = Devices.FirstOrDefault(m => m.Ip.Equals(ip));
        if (deviceModel == null)
        {
            return;
        }
        Task.Run(async () =>
        {
            var connected = await Client.CheckForConnection(deviceModel.Ip, 3000);
            if (connected)
            {
                deviceModel.SendThis = true;
                AddOrUpdateDevice(deviceModel.Name, deviceModel.Ip, deviceModel.SendThis);
                DisplayAlert("设备连接正常", "", "确定");
            }
            else
            {
                DisplayAlert("设备连接错误", "", "确定");
            }
        });
    }
    private void Client_DeviceNotFound(object sender, DeviceNotFoundEventArgs e)
    {
        var target = Devices.FirstOrDefault(d => d.Ip.Equals(e.IP));
        if(target == null)
        {
            DisplayAlert("警告", $"为连接设备");
        }
        else
        {
            DisplayAlert("警告", $"未找到设备{target.Name}({target.Ip})");
        }
        
    }
    private void Client_OnDeviceFound(object sender, DeviceFoundEventArgs e)
    {
        AddOrUpdateDevice(e.DeviceName, e.Ip, true);
    }
    static string SerilizeDevice(ObservableCollection<DeviceModel> devices)
    {
        return JsonSerializer.Serialize(devices);
    }
    static ObservableCollection<DeviceModel> DeserilizeDevice(string devices)
    {
        try
        {
            if (devices == null)
            {
                return null;
            }
            return JsonSerializer.Deserialize<ObservableCollection<DeviceModel>>(devices);
        }
        catch { }
        return null;
    }

    #endregion

    #region MainPageAction
    public async Task SendFile(FileType fileType)
    {
        Log($"start pick file of {fileType}");
        var res = await ServiceFactory.CreateFilePicker().PickFileAsync(fileType);
        if(res == null)
        {
            Log($"pick file cancelled");
            return;
        }
        Log($"file [{res.Name}] in [{res.Uri}] picked");

        _ = Client.SendFileAsync(res, GetFileTypeByFileName(res.Name), Devices.Where(d => d.SendThis).Select(d => d.Ip)).ConfigureAwait(false);
    }
    public async Task SendMultiFile()
    {
        Log($"start pick files");
        var ress = await ServiceFactory.CreateFilePicker().PickFilesAsync(FileActions.FileType.Any);
        if(ress == null)
        {
            Log($"pick files cancelled");
            return;
        }
        Log($"{ress.Count()} files picked");
        _ = Task.Run(async () =>
        {
            var targetDevice = Devices.Where(d => d.SendThis).Select(d => d.Ip).FirstOrDefault();
            if (targetDevice != null)
            {
                var files = ress.Select(r => (r, GetFileTypeByFileName(r.Name)));
                await Client.SendMultiFileAsync(files, targetDevice);
            }          
        }).ConfigureAwait(false);
    }
    public Task SendText(string text)
    {
        Log($"start sending text :\"{text}\"");
        var buffer = Encoding.UTF8.GetBytes(text);
        var stream = new MemoryStream(buffer);
        _ = Client.SendFileAsync(new FileActions.FilePickers.PickFileResult()
        {
            Name = $"文本{Guid.NewGuid()}.txt",
            Length = buffer.Length,
            Stream = stream,
        }, FileType.Text, Devices.Where(d => d.SendThis).Select(d => d.Ip)).ConfigureAwait(false);
        return Task.CompletedTask;
    }
    public Task ChooseDevice()
    {
        var diction = new Dictionary<string, object>
            {
                { nameof(Devices), Devices },
                {"Client",Client }
            };
        return Shell.Current.GoToAsync("ChooseDevice", true, diction);
    }
    #endregion

    #region RecievingFiles
    public void RemoveRecievingFile(uint fileId)
    {
        if (Client.UdpFiles.ContainsKey(fileId))
        {
            Client.UdpFiles.Remove(fileId);
            Client.SaveUdpFiles();
        }
        else if (Client.TcpClient.RecievingFiles.ContainsKey(fileId))
        {
            Client.StopRecieving(fileId);
            Client.TcpClient.RecievingFiles.Remove(fileId);
            Client.TcpClient.SaveHistoryToDataStore(DataStore);
        }
        var target = Files.FirstOrDefault(s => s.FileId == fileId);
        Files.Remove(target);
        Log($"recieving file {fileId}({target.Name}) deleted");
    }
    public void ClearRecievingFile()
    {
        Client.TcpClient.RecievingFiles.Clear();
        var targets = Client.UdpFiles.Where(t => !t.Value.IsSending).ToArray();
        foreach (var target in targets)
        {
            Client.UdpFiles.Remove(target.Key);
        }
        Client.SaveUdpFiles();
        Files.Clear();
        Client.TcpClient.SaveHistoryToDataStore(DataStore);
        Log($"recieving file cleared");
    }
    public async Task OpenRecievedFile(FileItem fileItem)
    {
        try
        {
            if (fileItem != null)
            {
                if (!string.IsNullOrEmpty(fileItem.Description))
                {
                    if (IsUri(fileItem.Description, out var uri))
                    {
                        opened = true;
                        await Browser.OpenAsync(uri, BrowserLaunchMode.External);
                    }
                    opened= await FileOpener.TryOpenAsync(fileItem);
                }
                else
                {
                    opened = await FileOpener.TryOpenAsync(fileItem);
                }
            }
        }
        catch
        {
            DisplayAlert("不支持", "");
        }
    }
    public Task InfoReciveFile(FileItem fileItem)
    {
        return Task.CompletedTask;
    }
    public async Task CopyText(FileItem fileItem)
    {
        if(fileItem.Description!= null)
        {
            await Clipboard.Default.SetTextAsync(fileItem.Description);
            DisplayAlert("复制成功", "");
        }
    }
    public void StopOrResumeRecievingFile(FileItem fileItem)
    {
        if (fileItem.Working)
        {
            Client.StopRecieving(fileItem.FileId);
        }
        else if(!SendingOrRecieving)
        {
            Client.ResumeRecieving(fileItem.FileId);
        }
    }
    public static bool IsUri(string value, out Uri uri)
    {
        return Uri.TryCreate(value, new UriCreationOptions()
        {
            DangerousDisablePathAndQueryCanonicalization = true,
        }, out uri);
    }
    #endregion
    #region SendingFiles
    public void RemoveSendingFile(uint fileId)
    {
        if (Client.UdpFiles.ContainsKey(fileId))
        {
            Client.UdpFiles.Remove(fileId);
            Client.SaveUdpFiles();
        }
        else if (Client.TcpClient.SendingFiles.ContainsKey(fileId))
        {
            Client.StopSending(fileId);
            Client.TcpClient.SendingFiles.Remove(fileId);
            Client.TcpClient.SaveHistoryToDataStore(DataStore);
        }
        var target = SendingFiles.FirstOrDefault(s => s.FileId == fileId);
        SendingFiles.Remove(target);
        Log($"sebding file {fileId}({target.Name}) deleted");
    }
    public void ClearSendingFile()
    {
        Client.TcpClient.SendingFiles.Clear();
        var targets=Client.UdpFiles.Where(t=>t.Value.IsSending).ToArray();
        foreach (var target in targets)
        {
            Client.UdpFiles.Remove(target.Key);
        }
        Client.SaveUdpFiles() ;
        SendingFiles.Clear();
        Client.TcpClient.SaveHistoryToDataStore(DataStore);
        Log($"sebding file cleared");
    }
    public void StopOrResumeSendingFile(FileItem fileItem)
    {
        if (fileItem.Working)
        {
            Client.StopSending(fileItem.FileId);
        }
        else if (!SendingOrRecieving)
        {
            Client.ResumeSending(fileItem.FileId);
        }
    }
    #endregion

    void LoadFileFromClient()
    {
        Files = new ObservableCollection<FileItem>();
        SendingFiles = new ObservableCollection<FileItem>();
        if (Client.TcpClient.RecievingFiles.Any())
        {
            foreach (var file in Client.TcpClient.RecievingFiles)
            {
                var item = new FileItem(this, false)
                {
                    FileId = file.Value.FileId,
                    Name = file.Value.FileName,
                    Length = file.Value.FileLength,
                    Path=file.Value.SavedPath,
                    CreateTime=file.Value.CreateTime,
                    Percent = (double)file.Value.Position / file.Value.FileLength,
                };
                if (item.Percent == 0)
                {
                    item.State = FileItemState.Waiting;
                }
                else if (item.Percent >= 1)
                {
                    item.State = FileItemState.Completed;
                }
                else
                {
                    item.State = FileItemState.Stopped;
                }
                Debug.WriteLine($"{file.Value.Position}-{item.Percent}");
                Files.Add(item);
            }
        }
        if (Client.TcpClient.SendingFiles.Any())
        {
            foreach (var file in Client.TcpClient.SendingFiles)
            {
                var item = new FileItem(this, true)
                {
                    FileId = file.Value.FileId,
                    Name = file.Value.FileName,
                    Length = file.Value.FileLength,
                    IsSendingFile = true,
                    Path=file.Value.FilePath,
                    CreateTime = file.Value.CreateTime,
                    Percent = (double)file.Value.Position / file.Value.FileLength,
                };
                if (item.Percent == 0)
                {
                    item.State = FileItemState.Waiting;
                }
                else if (item.Percent >= 1)
                {
                    item.State = FileItemState.Completed;
                }
                else
                {
                    item.State = FileItemState.Stopped;
                }
                SendingFiles.Add(item);
            }
        }
        if (Client.UdpFiles.Any())
        {
            foreach(var file in Client.UdpFiles)
            {
                var fileItem = new FileItem(this, file.Value.IsSending)
                {
                    FileId = file.Value.FileId,
                    Name = file.Value.FileName,
                    Length = file.Value.FileLength,
                    Percent = 1,
                    Description = file.Value.TextValue,
                    Path = file.Value.Path,
                    State=FileItemState.Completed,
                    CreateTime=file.Value.CreateTime,
                };
                if (fileItem.IsSendingFile)
                {
                    SendingFiles.Add(fileItem);
                }
                else
                {
                    Files.Add(fileItem);
                }
            }
        }
        Files = new ObservableCollection<FileItem>(Files.OrderByDescending(f => f.CreateTime).ToList());
        SendingFiles = new ObservableCollection<FileItem>(SendingFiles.OrderByDescending(f => f.CreateTime).ToList());
    }




    DateTime lastUpdate;
    private void Client_Recieving(object sender, FileResultEventArgs e)
    {
        Log($"On Recieing:state {e.State},fileId {e.FileId},fileName {e.FileName},fileLength {e.FileLength}");
        if (e.State == FileResultState.Start)
        {
            SendingOrRecieving = true;
            var target = Files.FirstOrDefault(f => f.FileId == e.FileId);
            if (target == null)
            {
                target = new FileItem(this,false)
                {
                    FileId = e.FileId,
                    Name = e.FileName,
                    Percent = 0,
                    Length = e.FileLength,
                    State = FileItemState.Waiting,
                    CreateTime=DateTime.Now,
                };
                lastUpdate = DateTime.Now;
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    Files.Insert(0,target);
                    Log($"add {e.FileId} to recieving files");
                });
            }
        }
        else if (e.State == FileResultState.Updating)
        {
            SendingOrRecieving = true;
            var target = Files.FirstOrDefault(f => f.FileId == e.FileId);
            if (target != null && target.State != FileItemState.Completed)
            {
                target.Working = true;
                target.State = FileItemState.Working;
                var delta = DateTime.Now - lastUpdate;
                lastUpdate = DateTime.Now;
                target.Speed =
                    (long)((e.Position - (target.Percent * target.Length))
                    /
                    (delta.TotalSeconds));
                target.Percent = (double)e.Position / e.FileLength;
                Log($"update recieving file prograss :percent {target.Percent},position {e.Position}");
            }
        }
        else if (e.State == FileResultState.Ending)
        {
            SendingOrRecieving = false;
            var target = Files.FirstOrDefault(f => f.FileId == e.FileId);
            if (target != null)
            {
                target.State = FileItemState.Completed;
                if (e.TextValue != null)
                {
                    target.Description = e.TextValue;
                }
                target.Path = e.SavedPath;
                target.Percent = 1;
                Log($"file {e.FileId}({e.FileName}) recieved success");

            }
            else
            {
                target = new FileItem(this, false)
                {
                    FileId = e.FileId,
                    Name = e.FileName,
                    Percent = 1,
                    Length = e.FileLength,
                    State = FileItemState.Completed,
                    CreateTime = DateTime.Now,
                };
                if (e.TextValue != null)
                {
                    target.Description=e.TextValue;
                }
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    Files.Insert(0, target);
                    Log($"add {e.FileId} to recieving files");
                });
            }
            if (AutoOpen && !opened)
            {
                Task.Run(() => OpenRecievedFile(target));
                return;
            }
            if (e.TextValue != null)
            {
                DisplayAlert("收到文本", e.TextValue);               
            }
            else
            {
                DisplayAlert("收到文件", e.SavedPath);
            }

        }
        else if (e.State == FileResultState.Stop)
        {
            SendingOrRecieving = false;
            var target = Files.FirstOrDefault(f => f.FileId == e.FileId);
            if (target != null)
            {
                target.Working = false;
                target.State = FileItemState.Stopped;
                Log($"file {e.FileId}({e.FileName}) recieved stopped");
                DisplayAlert("传输停止", $"{e.FileName}");
            }
        }
    }
    private void Client_Sending(object sender, FileResultEventArgs e)
    {
        Log($"On Sending:state {e.State},fileId {e.FileId},fileName {e.FileName},fileLength {e.FileLength}");
        if (e.State == FileResultState.Start)
        {
            SendingOrRecieving = true;
            var target = SendingFiles.FirstOrDefault(f => f.FileId == e.FileId);
            if (target == null)
            {
                target = new FileItem(this,true)
                {
                    FileId = e.FileId,
                    Name = e.FileName,
                    Percent = 0,
                    Length = e.FileLength,
                    State = FileItemState.Waiting,
                    CreateTime = DateTime.Now,
                };
                lastUpdate = DateTime.Now;
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    SendingFiles.Insert(0, target);
                    Log($"add {e.FileId} to sending files");
                });
            }
        }
        else if (e.State == FileResultState.Updating)
        {
            SendingOrRecieving = true;
            var target = SendingFiles.FirstOrDefault(f => f.FileId == e.FileId);
            if (target != null && target.State != FileItemState.Completed)
            {
                target.Working=true;
                target.State = FileItemState.Working;
                var delta = DateTime.Now - lastUpdate;
                lastUpdate = DateTime.Now;
                target.Speed =
                    (long)((e.Position - (target.Percent * target.Length))
                    /
                    (delta.TotalSeconds));
                target.Percent = (double)e.Position / e.FileLength;
                Log($"update sending file prograss :percent {target.Percent},position {e.Position}");
            }
        }
        else if (e.State == FileResultState.Ending)
        {
            SendingOrRecieving = false;
            var target = SendingFiles.FirstOrDefault(f => f.FileId == e.FileId);
            if (target != null)
            {
                target.State = FileItemState.Completed;
                if (e.TextValue != null)
                {
                    target.Description = e.TextValue;
                }
                target.Path = e.SavedPath;
                target.Percent = 1;
                DisplayAlert("发送完毕", e.FileName);
                Log($"file {e.FileId}({e.FileName}) sended success");
            }
            else
            {
                target = new FileItem(this, true)
                {
                    FileId = e.FileId,
                    Name = e.FileName,
                    Percent = 1,
                    Length = e.FileLength,
                    State = FileItemState.Completed,
                    CreateTime = DateTime.Now,
                    Path=e.SavedPath,
                };
                if (e.TextValue != null)
                {
                    target.Description = e.TextValue;
                }
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    SendingFiles.Insert(0, target);
                    Log($"add {e.FileId} to sending files");
                });
            }
        }
        else if (e.State == FileResultState.Stop)
        {
            SendingOrRecieving = false;
            var target = SendingFiles.FirstOrDefault(f => f.FileId == e.FileId);
            if (target != null)
            {
                target.State = FileItemState.Stopped;
                target.Working = false;
                DisplayAlert("传输停止", $"{e.FileName}");
                Log($"file {e.FileId}({e.FileName}) send stopped");
            }
        }
    }
    static bool alerting;
    public static void DisplayAlert(string title,string message,string cancel = "确定")
    {
        if (alerting)
        {
            return;
        }
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            alerting= true;
            await Shell.Current.CurrentPage.DisplayAlert(title, message, cancel);
            alerting= false;
        });
    }
    protected override void OnStart()
    {
        base.OnStart();
        Task.Run(() =>
        {
            FileSaver?.LoadFolderToSave(DataStore);
        });
    }

    public static FileType GetFileTypeByFileName(string fileName)
    {
        if (string.IsNullOrEmpty(fileName))
        {
            return FileType.Any;
        }
        var extension=Path.GetExtension(fileName).ToLower().Replace(".","");
        switch(extension)
        {
            default:
                return FileType.Any;
            case "txt":
            case "pdf":
            case "epub":
            case "mobi":
            case "excel":
            case "word":
            case "lrc":
                return FileType.Text;
            case "jpg":
            case "gif":
            case "jpeg":
            case "png":
            case "webp":
                return FileType.Image;
            case "mp3":
            case "wav":
            case "flac":

                return FileType.Audio;
            case "mp4":
            case "3gp":
            case "avi":
            case "mkv":
            case "rmvb":
            case "mov":
            case "wmv":
                return FileType.Video;

        }
    }
}
