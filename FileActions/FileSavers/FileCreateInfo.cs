using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UdpQuickShare.FileActions.FileSavers
{
    public class FileCreateInfo
    {
        public string Path { get; set; }
        public Stream Stream { get; set; }
        public FileType FileType { get; set; }
    }
}
