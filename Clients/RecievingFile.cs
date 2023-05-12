using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using UdpQuickShare.FileActions;

namespace UdpQuickShare.Clients
{
    public class RecievingFile
    {
        [JsonIgnore]
        public CancellationTokenSource CancellationTokenSource { get; set; }
        public uint FileId { get; set; }
        public string FileName { get; set; }
        public string SavedPath { get; set; }
        public long FileLength { get; set; }
        public FileType FileType { get; set; }
        public long Position { get; set; }
        
        public DateTime CreateTime { get; set; }
        [JsonIgnore]
        public IPEndPoint SenderIp { get; set; }
        
        public string IpString
        {
            get
            {
               return SenderIp.ToString();
            }
            set
            {
                SenderIp=IPEndPoint.Parse(value);
            }
        }
        public RecievingFile(CancellationTokenSource cancellationTokenSource,uint fileId, string fileName, string savedPath, long fileLength, FileType fileType, IPEndPoint senderIp)
        {
            CreateTime = DateTime.Now;
            CancellationTokenSource = cancellationTokenSource;
            FileId = fileId;
            FileName = fileName;
            SavedPath = savedPath;
            FileLength = fileLength;
            FileType = fileType;
            SenderIp = senderIp;
        }
    }
}
