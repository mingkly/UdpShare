using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UdpQuickShare.Protocols
{
    public enum CommandType
    {
        FoundDevice=0x1111_1111,
        DeviceReply=0x1111_1112,
        StopRecieving=0x1111_1113,
        StopSending=0x1111_1114,
        RecieverAskForResending=0x1111_1115,
        SenderReadyForResending=0x1111_1116,
        SenderAskForResending = 0x1111_1117,
        RecieverReadyForRecieving = 0x1111_1118,
        CheckForConnection = 0x1111_1119,
        ConnectionNormal = 0x1111_1120,
    }
}
