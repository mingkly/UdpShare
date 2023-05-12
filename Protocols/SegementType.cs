using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UdpQuickShare.Protocols
{
    public enum SegementType
    {
        Head=0x1101,
        Body=0x1102,
        Foot=0x1103,
        Reply=0x1104,
        Resend=0x1105,
        DeviceFound=0x1106,
        DeviceReply=0x1107,
        Command=0x1108,
    }
}
