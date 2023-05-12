using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using UdpQuickShare.Protocols;

namespace UdpQuickShare.Clients
{
    public class UdpRecievedEventArgs
    {
        public IPEndPoint IP { get; }
        public ISegement Segement { get; }
        public UdpRecievedEventArgs(IPEndPoint iP, ISegement segement)
        {
            IP = iP;
            Segement = segement;
        }
    }
}
