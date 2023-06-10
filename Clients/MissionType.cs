using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UdpQuickShare.Clients
{
    public enum MissionType
    {
        Sending,
        Recieving,
        WaitSending,
        WaitRecieving,
        WaitResumeSending,
        WaitResumeRecieving,
        SendingCompleted,
        RecievingComleted,
    }
}
