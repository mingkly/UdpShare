using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UdpQuickShare.Protocols
{
    public class StreamBodySegement:ISegement
    {
        public SegementType SegementType { get; }
        public uint FileId { get; }
        public uint FileOrder { get; }
        public Stream Stream { get; }
        public uint Index { get; }
        public uint Length { get; } 
        public StreamBodySegement(uint fileId, uint fileOrder, Stream stream, uint index, uint length)
        {
            SegementType = SegementType.Body;
            FileId = fileId;
            FileOrder = fileOrder;
            Stream = stream;
            Index = index;
            Length = length;
        }
    }
}
