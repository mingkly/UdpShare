using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UdpQuickShare.Protocols
{
    public interface IDecoder
    {
        ISegement Decode(ByteArrayBuffer buffer);
    }
}
