using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UdpQuickShare.FileActions.FilePickers
{
    public class PickFileResult
    {
        public string Name { get; set; }
        public string Uri { get; set; }
        public Stream Stream { get; set; }
        public long Length { get; set; }
    }
}
