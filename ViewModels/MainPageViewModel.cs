using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using UdpQuickShare.Clients;
using UdpQuickShare.FileActions;
using UdpQuickShare.Services;

namespace UdpQuickShare.ViewModels
{
    public class MainPageViewModel:ObservableObject
    {
        App app;
        public ObservableCollection<string> Messages { get; set; }
        string currentDevice;
        public string CurrentDevice
        {
            get => currentDevice;
            set=>SetProperty(ref currentDevice, value);
        }
        string targetDevice;
        public string TargetDevice
        {
            get => targetDevice;
            set => SetProperty(ref targetDevice, value);
        }
        bool canSend;
        public bool CanSend
        {
            get => canSend;
            set=>SetProperty(ref canSend, value);
        }
        Color currentDeviceTextColor;
        public Color CurrentDeviceTextColor
        {
            get => currentDeviceTextColor;
            set=>SetProperty<Color>(ref currentDeviceTextColor, value);
        }
        Color targetDeviceTextColor;
        public Color TargetDeviceTextColor
        {
            get => targetDeviceTextColor;
            set => SetProperty<Color>(ref targetDeviceTextColor, value);
        }
        public ICommand SendTextCommand { get; }
        public ICommand SendImageCommand { get; }
        public ICommand SendVideoCommand { get; }
        public ICommand SendAudioCommand { get; }
        public ICommand SendOtherCommand { get; }
        public ICommand SendMultiCommand { get; }
        public ICommand ChooseDeviceCommand { get; }
        public ICommand ToggleExposeCommand { get; }
        public MainPageViewModel():this(App.Instance)
        {
        }
        public MainPageViewModel(App app)
        {
            this.app = app;
            CanSend = true;
            SendTextCommand=new AsyncRelayCommand<string>(app.SendText, canExecute: (s) => !app.SendingOrRecieving);
            SendImageCommand = new AsyncRelayCommand(() => app.SendFile(FileType.Image), canExecute: () => !app.SendingOrRecieving);
            SendVideoCommand = new AsyncRelayCommand(() => app.SendFile(FileType.Video), canExecute: () => !app.SendingOrRecieving);
            SendAudioCommand = new AsyncRelayCommand(() => app.SendFile(FileType.Audio), canExecute: () => !app.SendingOrRecieving);
            SendOtherCommand = new AsyncRelayCommand(() => app.SendFile(FileType.Any), canExecute: () => !app.SendingOrRecieving);
            SendMultiCommand = new AsyncRelayCommand(app.SendMultiFile, canExecute: () => !app.SendingOrRecieving);
            ChooseDeviceCommand=new AsyncRelayCommand(app.ChooseDevice);
            ToggleExposeCommand = new Command(() =>
            {
                if (app.Client.Exposed)
                {
                    app.Client.DisableThisDevice();
                }
                else
                {
                    app.Client.ExposeThisDevice();
                }
            });
            CurrentDeviceTextColor = app.Client.Exposed ? Colors.LightGreen : Colors.Red;
            Messages = App.Messages;
            CurrentDevice = $"{App.DeviceName}({app.Client.UdpClient.CurrentIp?.Address})";
            TargetDevice = "选择发送设备";
            app.SendingDeviceChanged += App_SendingDeviceChanged;
            app.Client.CurrentIpChanged += Client_CurrentIpChanged;
            app.Client.ExposedChanged += Client_ExposedChanged;
            app.SendingOrRecievingChanged += (sender, e) => CanSend = !e;
        }

        private void Client_ExposedChanged(object sender, bool e)
        {
            CurrentDeviceTextColor = e ? Colors.LightGreen : Colors.Red;
        }

        private void Client_CurrentIpChanged(object sender, System.Net.IPEndPoint e)
        {
            CurrentDevice=$"{App.DeviceName}({e.Address})";
        }
        private void App_SendingDeviceChanged(object sender, EventArgs e)
        {
            var sendTarget = app.Devices.FirstOrDefault(d => d.SendThis);
            if (sendTarget != null)
            {
                TargetDevice = $"{sendTarget.Name}({sendTarget.Ip})";
            }
            else
            {
                TargetDevice = "选择发送设备";
            }
        }
    }
}
