using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UdpQuickShare.Protocols
{
    public interface ISegement
    {
        SegementType SegementType { get; }
        uint FileId { get; }
    }
}
