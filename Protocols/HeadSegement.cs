using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UdpQuickShare.FileActions;

namespace UdpQuickShare.Protocols
{
    public class HeadSegement:ISegement
    {
        public SegementType SegementType { get; }
        public string FileName { get; }
        public uint FileLength { get; }
        public uint FileId { get; }
        public FileType FileType { get; }

        public HeadSegement(string fileName,uint fileId,uint fileLength,FileType fileType)
        {
            SegementType = SegementType.Head;
            FileName = fileName;
            FileId = fileId;
            FileLength= fileLength;
            FileType = fileType;
        }
    }
}
