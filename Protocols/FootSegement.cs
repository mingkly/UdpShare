using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UdpQuickShare.Protocols
{
    public class FootSegement:ISegement
    {
        public SegementType SegementType { get; }
        public uint FileId { get; }
        public FootSegement( uint fileId)
        {
            SegementType = SegementType.Foot;
            FileId = fileId;
        }
    }
}
