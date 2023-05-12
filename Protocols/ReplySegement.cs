using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UdpQuickShare.Protocols
{
    public class ReplySegement : ISegement
    {
        public SegementType SegementType { get; }

        public uint FileId { get; }
        public ReplySegement( uint fileId)
        {
            SegementType = SegementType.Reply;
            FileId = fileId;
        }
    }
}
