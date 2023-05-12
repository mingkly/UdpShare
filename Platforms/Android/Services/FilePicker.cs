using Android.Content;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UdpQuickShare.FileActions;
using UdpQuickShare.FileActions.FilePickers;

namespace UdpQuickShare.FileActions.FilePickers
{
    public partial class FilePicker
    {
        MainActivity MainActivity => MainActivity.Instance;

        string ToMimeType(FileType fileType)
        {
            string mimeType;
            if (fileType == FileType.Text)
            {
                mimeType = "text/plain";
            }
            else if (fileType == FileType.Image)
            {
                mimeType = "image/*";
            }
            else if (fileType == FileType.Audio)
            {
                mimeType = "audio/*";
            }
            else if (fileType == FileType.Video)
            {
                mimeType = "video/*";
            }
            else
            {
                mimeType ="*/*";
            }
            return mimeType;
        }
        public Task<PickFileResult> PickFilePlatformAsync(FileType fileType)
        {
            string mimeType=ToMimeType(fileType);
            var intent = new Intent(Intent.ActionOpenDocument);
            intent.SetType(mimeType);
            MainActivity.PickFileTaskCompletionSource = new TaskCompletionSource<PickFileResult>();
            MainActivity.StartActivityForResult(intent, MainActivity.PickFileId);
            return MainActivity.PickFileTaskCompletionSource.Task;
        }
        public Task<IEnumerable<PickFileResult>> PickFilesPlatformAsync(FileType fileType)
        {
            string mimeType = ToMimeType(fileType);
            var intent = new Intent(Intent.ActionOpenDocument);
            intent.SetType(mimeType);
            intent.PutExtra(Intent.ExtraAllowMultiple, true);
            MainActivity.PickFilesTaskCompletionSource = new TaskCompletionSource<IEnumerable<PickFileResult>>();
            MainActivity.StartActivityForResult(intent, MainActivity.PickFilesId);
            return MainActivity.PickFilesTaskCompletionSource.Task;
        }
        public Stream OpenPickedFilePlatform(string uri)
        {
            try
            {
                return MainActivity.ContentResolver.OpenInputStream(Android.Net.Uri.Parse(uri));
            }
            catch(Exception ex)
            {
                Debug.WriteLine(ex);
            }
            return null;
        }
    }
}
