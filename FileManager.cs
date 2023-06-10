using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UdpQuickShare
{
    public class FileManager
    {
        public static bool UseDirectPath { get;set; }

        public static bool CanUseDirectPath => UseDirectPath && CanUseDirectPathPlatform();

        public static Stream OpenReadFile(string platformPath,string directPath)
        {
            if(CanUseDirectPath)
            {
                try
                {
                    return File.OpenRead(directPath);
                }
                catch
                {
                    return MKFilePicker.MKFilePicker.OpenPickedFile(platformPath, "r");
                }
                
            }
            else
            {
                return MKFilePicker.MKFilePicker.OpenPickedFile(platformPath, "r");
            }
        }
        public static Stream OpenWriteFile(string platformPath, string directPath)
        {
            if (CanUseDirectPath)
            {
                return File.OpenWrite(directPath);
            }
            else
            {
                return MKFilePicker.MKFilePicker.OpenPickedFile(platformPath, "w");
            }
        }
        public static Stream OpenReadWriteFile(string platformPath, string directPath)
        {
            if (CanUseDirectPath)
            {
                return File.Open(directPath,FileMode.OpenOrCreate);
            }
            else
            {
                return MKFilePicker.MKFilePicker.OpenPickedFile(platformPath, "rw");
            }
        }

        static bool CanUseDirectPathPlatform()
        {
#if WINDOWS
            return true;
#elif ANDROID
            return MauiFileSaver.Platforms.Android.Services.FileSaver.CanUseDirectFileApi();
#else
            return false;
#endif
        }
        public static Task<bool> RequsetUseDirectPathNeed()
        {
            if (CanUseDirectPath)
            {
                return Task.FromResult(true);
            }
#if ANDROID
            return MauiFileSaver.Platforms.Android.Services.FileSaver.RequestUseDirerctFileApi();
#else
            return Task.FromResult(true);
#endif
        }
    }
}
