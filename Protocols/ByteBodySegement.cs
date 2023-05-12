using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UdpQuickShare.Protocols
{
    public class ByteBodySegement:ISegement
    {
        public SegementType SegementType { get; }
        public uint FileId { get; }
        public uint FileOrder { get; }
        public byte[] RawData { get; }
        public ByteBodySegement( uint fileId, uint fileOrder, byte[] rawData)
        {
            SegementType = SegementType.Foot;
            FileId = fileId;
            FileOrder = fileOrder;
            RawData = rawData;
        }
    }
}
