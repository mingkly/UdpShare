using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace UdpQuickShare.Clients
{
    public class DeviceFoundEventArgs
    {
        public string DeviceName { get; }
        public IPEndPoint Ip { get; }
        public DeviceFoundEventArgs(IPEndPoint ip, string deviceName)
        {
            Ip = ip;    
            DeviceName = deviceName;
        }
    }
}
