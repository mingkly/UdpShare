using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using UdpQuickShare.Clients;
using UdpQuickShare.FileActions;
using UdpQuickShare.ViewModels;

namespace UdpQuickShare
{
    public class DeviceManager
    {
        public event EventHandler SendingDeviceChanged;
        public IDataStore DataStore { get; }
        public ShareClient Client { get; }
        public DeviceModel SendingDevice => Devices.FirstOrDefault(d => d.SendThis);
        public IPEndPoint SendingDeviceIP => Devices.FirstOrDefault(d=>d.SendThis)?.Ip;
        public App App { get; }

        bool searchingDevice;
        public DeviceManager(IDataStore dataStore,ShareClient shareClient, App app)
        {
            DataStore = dataStore;
            Client = shareClient;
            App = app;
            Client.DeviceNotFound += Client_DeviceNotFound;
            Client.OnDeviceFound += Client_OnDeviceFound;
        }
        void Log(string msg)=>App.Log(msg);
        public ObservableCollection<DeviceModel> Devices { get; private set; }
        #region Devices
        public void LoadDevice()
        {
            Devices = DataStore.Get<ObservableCollection<DeviceModel>>(nameof(Devices)) ?? new ObservableCollection<DeviceModel>();
            foreach (var device in Devices)
            {
                device.SendThis = false;
                Log($"{device.Name}-{device.SendThis}");
            }
        }
        public void AddOrUpdateDevice(string deviceName, IPEndPoint ip, bool sendThis)
        {
            var existed = Devices.FirstOrDefault(x => x.Ip.Equals(ip));
            var hasSendDevice = Devices.Any(x => x.SendThis);
            if (existed == null)
            {
                Devices.Add(new DeviceModel()
                {
                    Ip = ip,
                    Name = deviceName,
                    SendThis = !hasSendDevice,
                });
            }
            else
            {
                existed.Name = deviceName;
                existed.SendThis = sendThis &&(existed.SendThis|| !hasSendDevice);
            }
            DataStore.Save(nameof(Devices), Devices);
            SendingDeviceChanged?.Invoke(Devices, EventArgs.Empty);
        }
        public bool DeleteDevice(IPEndPoint ip)
        {
            var existed = Devices.FirstOrDefault(x => x.Ip.Equals(ip));
            if (existed != null)
            {
                Devices.Remove(existed);
                DataStore.Save(nameof(Devices), Devices);
                SendingDeviceChanged?.Invoke(Devices, EventArgs.Empty);
                Log($"Device {existed.Name}({existed.Ip}) deleted success");
                return true;
            }
            return false;
        }
        internal void ChooseDevice(IPEndPoint ip)
        {
            if (searchingDevice)
            {
                return;
            }
            searchingDevice = true;
            foreach (var m in Devices)
            {
                m.SendThis = false;
            }
            var deviceModel = Devices.FirstOrDefault(m => m.Ip.Equals(ip));
            if (deviceModel == null)
            {
                searchingDevice = false;
                return;
            }
            Task.Run(async () =>
            {
                var connected = await Client.CheckForConnection(deviceModel.Ip, 3000);
                if (connected)
                {
                    deviceModel.SendThis = true;
                    AddOrUpdateDevice(deviceModel.Name, deviceModel.Ip, deviceModel.SendThis);
                    App.DisplayAlert("设备连接正常", "", "确定");
                }
                else
                {
                    App.DisplayAlert("设备连接错误", "", "确定");
                }
                searchingDevice = false;
            });
        }
        private void Client_DeviceNotFound(object sender, DeviceNotFoundEventArgs e)
        {
            var target = Devices.FirstOrDefault(d => d.Ip.Equals(e.IP));
            if (target == null)
            {
                App.DisplayAlert("警告", $"为连接设备");
            }
            else
            {
                App.DisplayAlert("警告", $"未找到设备{target.Name}({target.Ip})");
            }

        }
        private void Client_OnDeviceFound(object sender, DeviceFoundEventArgs e)
        {
            AddOrUpdateDevice(e.DeviceName, e.Ip, true);
        }
        #endregion

    }
}
