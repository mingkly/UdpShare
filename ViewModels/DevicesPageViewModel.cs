using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using UdpQuickShare.Clients;


namespace UdpQuickShare.ViewModels
{
    public class DevicesPageViewModel : ViewModelBase, IQueryAttributable
    {
        public ObservableCollection<DeviceModel> Devices { get; set; }
        ShareClient client;
        public ICommand FindDeviceCommand { get; }

        public DevicesPageViewModel()
        {
            FindDeviceCommand = new Command(FindDevice);
        }
        public void FindDevice()
        {
            client?.FindDevices();
        }


        public void ApplyQueryAttributes(IDictionary<string, object> query)
        {
            client = query["Client"] as ShareClient;
            Devices = query["Devices"] as ObservableCollection<DeviceModel>;
            OnPropertyChanged(nameof(Devices));
        }
    }
}
