using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UdpQuickShare.FileActions;

namespace UdpQuickShare.ViewModels
{
    public class FolderItem:ViewModelBase
    {
        public FileType FileType { get; set; }
        string name;
        public string Name
        {
            get => name;
            set=>Set(ref name, value);
        }
        string path;
        public string Path
        {
            get => path; set => Set(ref path, value);
        }
    }
}
