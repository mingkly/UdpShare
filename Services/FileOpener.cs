using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UdpQuickShare.ViewModels;
using Microsoft.Maui.Controls.PlatformConfiguration;
#if ANDROID
using Android.Content;
#endif

namespace UdpQuickShare.Services
{
    internal class FileOpener
    {
        public static Task<bool> TryOpenAsync(FileItem fileItem)
        {
            try
            {
#if ANDROID
                    Android.Net.Uri uri=fileItem.Path.StartsWith("content")?Android.Net.Uri.Parse(fileItem.Path):Android.Net.Uri.Parse($"file:/{fileItem.Path}");
                    var context = Android.App.Application.Context;
                    var intent = new Intent(Intent.ActionView,uri );
                    intent.AddFlags(ActivityFlags.NewTask);
                    if(!fileItem.Path.StartsWith("content"))
                    {
                       var ext = System.IO.Path.GetExtension(fileItem.Name);
                       var type= Android.Webkit.MimeTypeMap.Singleton.GetMimeTypeFromExtension(ext.Replace(".", ""));
                       intent.SetType(type); 
                    }

                    context.StartActivity(intent);
                    return Task.FromResult(true);


#else
                return Launcher.TryOpenAsync(fileItem.Path);
#endif

            }
            catch(Exception ex)
            {
                App.Log(ex);
            }
            return Task.FromResult(false);
        }
    }
}
