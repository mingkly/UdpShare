using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace UdpQuickShare.Clients
{
    public class SendingFile
    {
        [JsonIgnore]
        public CancellationTokenSource CancellationTokenSource { get; set; }
        public uint FileId { get; set; }
        public string FilePath { get; set; }
        public string FileName { get; set; }
        public long FileLength { get; set; }
        public long Position { get; set; }
        public DateTime CreateTime { get; set; }
        [JsonIgnore]
        public IPEndPoint IPEndPoint { get; set; }
        public string IpString
        {
            get
            {
                return IPEndPoint.ToString();
            }
            set
            {
                IPEndPoint = IPEndPoint.Parse(value);
            }
        }
        public SendingFile(CancellationTokenSource cancellationTokenSource, uint fileId, string filePath, string fileName, long fileLength,IPEndPoint iPEndPoint)
        {
            CreateTime = DateTime.Now;
            CancellationTokenSource = cancellationTokenSource;
            FileId = fileId;
            FilePath = filePath;
            FileName = fileName;
            FileLength = fileLength;
            IPEndPoint= iPEndPoint;
        }


    }
}
