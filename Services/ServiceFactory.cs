using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UdpQuickShare.FileActions.FileSavers;

namespace UdpQuickShare.Services
{
    public static class ServiceFactory
    {
        public static Func<UdpQuickShare.FileActions.FilePickers.IFilePicker> FilePickerFunc;
        public static  UdpQuickShare.FileActions.FilePickers.IFilePicker CreateFilePicker()
        {
            if(FilePickerFunc!=null) return FilePickerFunc();
            return new UdpQuickShare.FileActions.FilePickers.FilePicker();
        }
        public static Func<IFileSaver> FileSaverFunc;
        public static IFileSaver CreateFileSaver()
        {
            if(FileSaverFunc!=null) return FileSaverFunc();
            return new FileSaver();
        }
    }
}
