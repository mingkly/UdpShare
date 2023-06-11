
using MKFilePicker;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using UdpQuickShare.Clients;
using UdpQuickShare.FileActions;
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

    public ObservableCollection<ClientMission> Missions { get; private set; }
    public static IDataStore DataStore { get; private set; }
    public DeviceManager DeviceManager { get; }
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
        DataStore = new FileDataStore();
        FileSaveManager.DataStore = DataStore;
        var encoder = new DefaultEncoder();
        var decoder = new DefaultDecoder();
        LoadFileFromClient();
        Client = new ShareClient(encoder, decoder,  DataStore, UdpPort, TcpPort, BufferSize,Missions);
        DeviceManager=new DeviceManager(DataStore, Client,this);

        LoadUseDirectPath(DataStore);
        DeviceManager.LoadDevice();
        Client.OnMissionHandled += Client_OnMissionHandled;
        Instance = this;
        Client.SendingError += Client_SendingError;
    }



    private void Client_SendingError(object sender, ClientMission e)
    {
        DisplayAlert("错误", $"{e.FileName}-{e.FilePlatformPath}-{e.FilePath}");
    }

    protected override void OnResume()
    {
        base.OnResume();
        opened = false;
        
    }
    void LoadUseDirectPath(IDataStore dataStore)
    {
        var useDirectPath = dataStore.Get<bool>("UseDirectPath");
        FileManager.UseDirectPath = useDirectPath;
    }
    #region Messages
    static void MessagesAdd(string message)
    {
        try
        {
            MainThread.BeginInvokeOnMainThread(() => Messages.Insert(0,message));
        }
        catch
        {
            //Messages.Add(message);
            //Log(ex);
            //Log(message);
        }     
    }
    public static void Log(string message)
    {
        

#if DEBUG
        MessagesAdd(message);
        Debug.WriteLine(message);
#endif

    }
    public static void Log(object value)
    {

#if DEBUG
        DisplayAlert("", value.ToString());
        MessagesAdd(value.ToString());
        Debug.WriteLine(value);
#endif
    }
    #endregion
    #region Devices
    public void AddOrUpdateDevice(string deviceName, IPEndPoint ip, bool sendThis)=> DeviceManager.AddOrUpdateDevice(deviceName, ip, sendThis);
    public bool DeleteDevice(IPEndPoint ip)=> DeviceManager.DeleteDevice(ip);
    internal void ChooseDevice(IPEndPoint ip)=>DeviceManager.ChooseDevice(ip);
    #endregion

    #region MainPageAction
    static FilePickerFileType[] FilePickerFileTypes = new FilePickerFileType[]
    {
        new FilePickerFileType(new Dictionary<DevicePlatform,IEnumerable<string>>
        {
            {DevicePlatform.Android,new string[]{"image/*"} },
            {DevicePlatform.WinUI,new string[]{"*.png", "*.jpg", "*.jpeg", "*.webp" } }
        }),
        new FilePickerFileType(new Dictionary<DevicePlatform,IEnumerable<string>>
        {
            {DevicePlatform.Android,new string[]{"audio/*"} },
            {DevicePlatform.WinUI,new string[]{ "*.mp3", "*.wav", "*.flac", "*.m4a", } }
        }),
        new FilePickerFileType(new Dictionary<DevicePlatform,IEnumerable<string>>
        {
            {DevicePlatform.Android,new string[]{"video/*"} },
            {DevicePlatform.WinUI,new string[]{ "*.mp4", "*.rmvb", "*.mkv", "*.3gp", "*.wmv", "*.mov"} }
        }),
        new FilePickerFileType(new Dictionary<DevicePlatform,IEnumerable<string>>
        {
            {DevicePlatform.Android,new string[]{"*/*"} },
            {DevicePlatform.WinUI,new string[]{ "" } }
        }),
        new FilePickerFileType(new Dictionary<DevicePlatform,IEnumerable<string>>
        {
            {DevicePlatform.Android,new string[]{"text/*","application/*"} },
            {DevicePlatform.WinUI,new string[]{ "*.txt","*.csv", "*.lrc", "*.srt", "*.ass", } }
        }),
    };
    static FilePickOptions MapFilePickOptions(FileType fileType)
    {
        var option = new FilePickOptions();
        option.FileTypes = FilePickerFileTypes[(int)fileType];
        return option;
    }

    public async Task SendFile(FileType fileType)
    {
        Log($"start pick file of {fileType}");
        var res = await MKFilePicker.MKFilePicker.PickFileAsync(MapFilePickOptions(fileType));
        if(res == null)
        {
            Log($"pick file cancelled");
            return;
        }
        Log($"file [{res.FileName}] in [{res.FullPath}] picked");
        var mission = new ClientMission(MissionType.WaitSending,
            (uint)res.GetHashCode(),
            res.FileName,
            res.FullPath,
            res.PlatformPath,
            GetFileTypeByFileName(res.FileName),
            0,
            DeviceManager.SendingDeviceIP);
        _ = Client.HandleMission(mission);
    }
    public async Task SendMultiFile()
    {
        Log($"start pick files");
        var ress = await MKFilePicker.MKFilePicker.PickFilesAsync(MapFilePickOptions(FileType.Any));
        if (ress == null)
        {
            Log($"pick files cancelled");
            return;
        }
        Log($"{ress.Count()} files picked");
        _ = Task.Run(async () =>
        {
            foreach(var res in ress)
            {
                var mission = new ClientMission(MissionType.WaitSending,
                    (uint)res.GetHashCode(),
                    res.FileName,
                    res.FullPath,
                    res.PlatformPath,
                    GetFileTypeByFileName(res.FileName),
                    0,
                    DeviceManager.SendingDeviceIP);
                await Client.HandleMission(mission);
                await Task.Delay(1000);
            }      
        });
    }
    public Task SendText(string text)
    {
        Log($"start sending text :\"{text}\"");
        var mission = new ClientMission(text, DeviceManager.SendingDeviceIP);
        _ = Client.HandleMission(mission);
        return Task.CompletedTask;
    }
    public Task ChooseDevice()
    {
        var diction = new Dictionary<string, object>
            {
                { nameof(Devices), DeviceManager.Devices },
                {"Client",Client }
            };
        return Shell.Current.GoToAsync("ChooseDevice", true, diction);
    }
#endregion

    #region RecievingFiles
    public void RemoveRecievingFile(uint fileId)
    {
        var target = Missions.FirstOrDefault(m => m.FileId == fileId);      
        if (target!=null)
        {
            Client.StopRecieving(fileId);
            Missions.Remove(target);
            SaveFiles();
        }
        var target2 = Files.FirstOrDefault(s => s.FileId == fileId);
        Files.Remove(target2);
        Log($"recieving file {fileId}({target2.Name}) deleted");
    }
    public void ClearRecievingFile()
    {
        var targets = Missions.Where(m => !IsSendingFile(m)).ToArray();
        foreach (var target in targets)
        {
            Missions.Remove(target);
        }
        SaveFiles();
        Files.Clear();
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
    public static Task InfoReciveFile(FileItem fileItem)
    {
        Log(fileItem.Path);
        return Task.CompletedTask;
    }
    public static async Task CopyText(FileItem fileItem)
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
        var target = Missions.FirstOrDefault(m => m.FileId == fileId);
        if (target != null)
        {
            Client.StopRecieving(fileId);
            Missions.Remove(target);
            SaveFiles();
        }
        var target2 = SendingFiles.FirstOrDefault(s => s.FileId == fileId);
        SendingFiles.Remove(target2);
        Log($"sending file {fileId}({target2.Name}) deleted");
    }
    public void ClearSendingFile()
    {
        var targets = Missions.Where(m => IsSendingFile(m)).ToArray();
        foreach (var target in targets)
        {
            Missions.Remove(target);
        }
        SaveFiles();
        SendingFiles.Clear();
        Log($"sending file cleared");
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
        try
        {
            Missions = DataStore.Get<ObservableCollection<ClientMission>>(nameof(Missions))??new ObservableCollection<ClientMission>();
        }
        catch
        {
            Missions = new ObservableCollection<ClientMission>();
        }
        Files = new ObservableCollection<FileItem>();
        SendingFiles = new ObservableCollection<FileItem>();
        if (Missions.Any())
        {
            foreach (var file in Missions.OrderByDescending(m=>m.CreateTime))
            {
                var item = ConvertToFileItem(file);
                Debug.WriteLine($"{file.Position}-{item.Percent}");
                if (item.IsSendingFile)
                {
                    SendingFiles.Add(item);
                }
                else
                {
                    Files.Add(item);
                }
            }
        }
    }
    void SaveFiles()
    {
        //Log($"missions:{string.Join(",", Missions.Select(s => s.FileName))}");
        DataStore.Save(nameof(Missions), Missions);
    }
    void TryAddMission(ClientMission mission)
    {
        if (Missions.FirstOrDefault(a => a.FileId == mission.FileId) == null)
        {
            Missions.Add(mission);
            SaveFiles();
        }
    }
    static bool IsSendingFile(ClientMission file)
    {
        return file.Type == MissionType.Sending ||
                    file.Type == MissionType.WaitSending ||
                    file.Type == MissionType.WaitResumeSending ||
                    file.Type == MissionType.SendingCompleted;
    }
    FileItem ConvertToFileItem(ClientMission file)
    {
        var isSending= IsSendingFile(file);
        var item = new FileItem(this, isSending)
        {
            FileId = file.FileId,
            Name = file.FileName,
            Length = file.FileLength,
            Path = file.FilePlatformPath,
            CreateTime = file.CreateTime,
            Percent = (double)file.Position / file.FileLength,
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
        return item;
    }


    DateTime lastUpdate;
    
    private void Client_OnMissionHandled(object sender, ClientMission e)
    {
        if (e.Type == MissionType.WaitSending)
        {
            SendingOrRecieving = true;
            var target = Files.FirstOrDefault(f => f.FileId == e.FileId);
            if (target == null)
            {
                TryAddMission(e);
                target = ConvertToFileItem(e);
                lastUpdate = DateTime.Now;
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    SendingFiles.Insert(0, target);
                    Log($"add {e.FileId} to recieving files");
                });
            }
        }
        else if(e.Type== MissionType.Sending)
        {
            SendingOrRecieving = true;
            var target = SendingFiles.FirstOrDefault(f => f.FileId == e.FileId);
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
                Log($"update sending file prograss :percent {target.Percent},position {e.Position}");
            }
        }
        else if (e.Type == MissionType.WaitResumeSending)
        {
            SendingOrRecieving = false;
            var target = SendingFiles.FirstOrDefault(f => f.FileId == e.FileId);
            if (target != null)
            {
                target.State = FileItemState.Stopped;
                target.Working = false;
                SaveFiles();
                DisplayAlert("传输停止", $"{e.FileName}");
                Log($"file {e.FileId}({e.FileName}) send stopped");
            }
        }
        else if (e.Type == MissionType.SendingCompleted)
        {
            SendingOrRecieving = false;
            var target = SendingFiles.FirstOrDefault(f => f.FileId == e.FileId);
            if (target != null)
            {
                target.State = FileItemState.Completed;
                if (e.Text != null)
                {
                    target.Description = e.Text;
                }
                target.Path = e.FilePlatformPath;
                target.Percent = 1;
                DisplayAlert("发送完毕", e.FileName);
                Log($"file {e.FileId}({e.FileName}) sended success");
            }
            else
            {
                TryAddMission(e);
                target = ConvertToFileItem(e);
                if (e.Text != null)
                {
                    target.Description = e.Text;
                }
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    SendingFiles.Insert(0, target);
                    Log($"add {e.FileId} to sending files");
                });
            }
            e.Position = e.FileLength;
            SaveFiles();
        }
        else if (e.Type == MissionType.WaitRecieving)
        {
            SendingOrRecieving = true;
            var target = Files.FirstOrDefault(f => f.FileId == e.FileId);
            if (target == null)
            {
                TryAddMission(e);
                target = ConvertToFileItem(e);
                lastUpdate = DateTime.Now;
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    Files.Insert(0, target);
                    Log($"add {e.FileId} to recieving files");
                });
            }
        }
        else if (e.Type == MissionType.Recieving)
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
        else if (e.Type == MissionType.WaitResumeRecieving)
        {
            SendingOrRecieving = false;
            var target = Files.FirstOrDefault(f => f.FileId == e.FileId);
            if (target != null)
            {
                target.Working = false;
                target.State = FileItemState.Stopped;
                SaveFiles();
                Log($"file {e.FileId}({e.FileName}) recieved stopped");
                DisplayAlert("传输停止", $"{e.FileName}");
            }
        }
        else if (e.Type == MissionType.RecievingComleted)
        {
            SendingOrRecieving = false;
            var target = Files.FirstOrDefault(f => f.FileId == e.FileId);
            if (target != null)
            {
                target.State = FileItemState.Completed;
                if (e.Text != null)
                {
                    target.Description = e.Text;
                }
                target.Path = e.FilePlatformPath;
                target.Percent = 1;
                Log($"file {e.FileId}({e.FileName}) recieved success");
            }
            else
            {
                TryAddMission(e);
                target = ConvertToFileItem(e);
                if (e.Text != null)
                {
                    target.Description = e.Text;
                }
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    Files.Insert(0, target);
                    Log($"add {e.FileId} to recieving files");
                });
            }
            e.Position = e.FileLength;
            SaveFiles();
            if (AutoOpen && !opened)
            {
                Task.Run(() => OpenRecievedFile(target));
                return;
            }
            if (e.Text!= null)
            {
                DisplayAlert("收到文本", e.Text);
            }
            else
            {
                DisplayAlert("收到文件", e.FilePath);
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
        Task.Run(async () =>
        {
            await Client.SetUp();
            Client.ExposeThisDevice();
        });
    }

    public static FileType GetFileTypeByFileName(string fileName)
    {
        if (string.IsNullOrEmpty(fileName))
        {
            return FileType.Any;
        }
        var extension=Path.GetExtension(fileName).ToLower().Replace(".","");
        return extension switch
        {
            "txt" or "pdf" or "epub" or "mobi" or "excel" or "word" or "lrc" => FileType.Text,
            "jpg" or "gif" or "jpeg" or "png" or "webp" => FileType.Image,
            "mp3" or "wav" or "flac" => FileType.Audio,
            "mp4" or "3gp" or "avi" or "mkv" or "rmvb" or "mov" or "wmv" => FileType.Video,
            _ => FileType.Any,
        };
    }
}
