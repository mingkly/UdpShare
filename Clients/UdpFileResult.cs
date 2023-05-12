using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UdpQuickShare.Clients
{
    public class UdpFileResult
    {
        public uint FileId { get; set; }
        public string FileName { get; set; }
        public string Path { get; set; }
        public bool IsSending { get; set; }
        public string TextValue { get; set; }
        public long FileLength { get; set; }
        public DateTime CreateTime { get; set; }
        public UdpFileResult()
        {
            CreateTime= DateTime.Now;
        }

    }
}
