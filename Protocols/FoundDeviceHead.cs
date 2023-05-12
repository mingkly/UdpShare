using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UdpQuickShare.Protocols
{
    public class FoundDeviceHead:ISegement
    {
        public static int DefaultId = 0x22222222;
        public int Id { get;}
        public SegementType SegementType { get; }
        public string DeviceName { get; }

        public uint FileId => 0;

        public FoundDeviceHead(string deviceName)
        {
            Id= DefaultId;
            SegementType = SegementType.DeviceFound;
            DeviceName = deviceName;
        }
    }
}
