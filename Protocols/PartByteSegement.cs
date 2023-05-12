using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UdpQuickShare.Protocols
{
    public class PartByteSegement:ISegement
    {
        public SegementType SegementType { get; }
        public uint FileId { get; }
        public uint FileOrder { get; }
        public byte[] TotalData { get; }
        public int Offset { get; }
        public int Count { get; }
        public PartByteSegement(uint fileId, uint fileOrder, byte[] rawData,int offset,int count)
        {
            SegementType = SegementType.Foot;
            FileId = fileId;
            FileOrder = fileOrder;
            TotalData= rawData;
            Offset= offset;
            Count = count;
        }
    }
}
