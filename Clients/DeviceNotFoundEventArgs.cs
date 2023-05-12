using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace UdpQuickShare.Clients
{
    public class DeviceNotFoundEventArgs
    {
        public IPEndPoint IP {  get; }
        public DeviceNotFoundEventArgs(IPEndPoint ip)
        {
            this.IP = ip;
        }
    }
}
