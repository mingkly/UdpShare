using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using UdpQuickShare.FileActions;
using UdpQuickShare.Protocols;

namespace UdpQuickShare.Clients
{
    public class ClientMission
    {
        public MissionType Type { get; set; }
        [JsonIgnore]
        public CancellationTokenSource CancellationTokenSource { get; set; }
        public uint FileId { get; set; }
        public string FilePath { get; set; }
        public string FilePlatformPath { get; set; }
        public string FileName { get; set; }
        public FileType FileType { get; set; }
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
        public string Text { get; set; }
        public ClientMission() { }
        public ClientMission(MissionType missionType, uint fileId, string filePath, string filePlatformPath,FileType fileType, long fileLength, long position, IPEndPoint iPEndPoint)
        {
            Type=missionType;
            FileId = fileId;
            FilePath = filePath;
            FilePlatformPath = filePlatformPath;
            FileName = Path.GetFileName(filePath);
            FileType =fileType;
            FileLength = fileLength;
            Position = position;
            CreateTime =DateTime.Now;
            IPEndPoint = iPEndPoint;
        }
        public ClientMission(MissionType missionType, uint fileId, string fileName, FileType fileType, long fileLength, long position, IPEndPoint iPEndPoint)
        {
            Type = missionType;
            FileId = fileId;
            FileName = fileName;
            FileType = fileType;
            FileLength = fileLength;
            Position = position;
            CreateTime = DateTime.Now;
            IPEndPoint = iPEndPoint;
        }
        public ClientMission(MissionType missionType, uint fileId,string fileName, string filePath, string filePlatformPath, FileType fileType,long position, IPEndPoint iPEndPoint)
        {
            Type = missionType;
            FileId = fileId;
            FilePath = filePath;
            FilePlatformPath = filePlatformPath;
            FileName =fileName;
            FileType = fileType;

            Position = position;
            CreateTime = DateTime.Now;
            IPEndPoint = iPEndPoint;
            using var stream = OpenRead();
            FileLength =stream.Length;
        }
        public ClientMission(string text,IPEndPoint iPEndPoint)
        {
            Type = MissionType.WaitSending;
            IPEndPoint = iPEndPoint;
            Text= text;
            FileId= (uint)text.GetHashCode();
            CreateTime= DateTime.Now;
            Position = 0;
            FileType = FileType.Text;
            FileLength=Encoding.UTF8.GetByteCount(text);
            FileName= text?.Substring(0, Math.Min(5, text.Length));
            FilePath = FileName;
            FilePlatformPath= FileName;
        }
        public Stream OpenRead()
        {
            if (Text != null)
            {
                var buffer = Encoding.UTF8.GetBytes(Text);
                var stream = new MemoryStream(buffer);
                return stream;
            }
            else
            {
                return FileManager.OpenReadFile(FilePlatformPath, FilePath);
            }
        }
        public Stream OpenWrite()
        {
            return FileManager.OpenWriteFile(FilePlatformPath, FilePath);
        }
        public bool FileExist()
        {
            try
            {
                using var stream =OpenRead();
                if (stream == null)
                {
                    return false;
                }
                return true;
            }
            catch
            {

            }
            return false;
        }
    }
}
