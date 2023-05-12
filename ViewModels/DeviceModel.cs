using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows.Input;

namespace UdpQuickShare.ViewModels
{
    public class DeviceModel:ViewModelBase
    {
        string name;
        public string Name
        {
            get { return name; }
            set=>Set(ref name, value);
        }
        IPEndPoint ip;
        [JsonIgnore]
        public IPEndPoint Ip
        {
            get { return ip; }
            set => Set(ref ip, value);
        }
        public string IpString
        {
            get=>Ip.ToString();
            set
            {
                Ip=IPEndPoint.Parse(value);
            }
        }
        bool sendThis;
        public bool SendThis
        {
            get => sendThis;
            set => Set(ref sendThis, value);
        }
        [JsonIgnore]
        public ICommand DeleteCommand { get; }
        [JsonIgnore]
        public ICommand ChooseCommand { get; }
        private readonly App app;
        public DeviceModel(App app)
        {
            this.app= app;
            DeleteCommand=new Command(Delete);
            ChooseCommand=new Command(Choose);
        }
        public DeviceModel()
        {
            app=App.Current as App;
            DeleteCommand = new Command(Delete);
            ChooseCommand = new Command(Choose);
        }
        public void Delete()
        {
            app.DeleteDevice(Ip);
        }
        public void Choose()
        {
            app.ChooseDevice(Ip);
        }
    }
}
