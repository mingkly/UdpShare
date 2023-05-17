using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using AndroidX.AppCompat.App;
using AndroidX.DocumentFile.Provider;
using UdpQuickShare.FileActions.FilePickers;
using Uri = Android.Net.Uri;
namespace UdpQuickShare
{
    [Activity(MainLauncher = false, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    internal class FilePickerActivity:Activity
    {
        public static TaskCompletionSource<PickFileResult> PickFileTaskCompletionSource { get; set; }
        public static int PickFileId = 1;
        public static TaskCompletionSource<IEnumerable<PickFileResult>> PickFilesTaskCompletionSource { get; set; }
        public static int PickFilesId = 2;
        public static TaskCompletionSource<PickFileResult> PickFolderTaskCompletionSource { get; set; }
        public static int PickFolderId = 3;
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            var intent = new Intent(Intent.ActionOpenDocument);
            intent.SetType("*/*");
            StartActivityForResult(intent, PickFileId);
            this.SetContentView(new Android.Widget.LinearLayout(this));
        }

        protected override void OnActivityResult(int requestCode, Result resultCode, Intent data)
        {
            base.OnActivityResult(requestCode, resultCode, data);
            if (requestCode == PickFileId)
            {
                try
                {
                    if (resultCode == Result.Ok)
                    {
                        var uri = data.Data;
                        var takeFlags = ActivityFlags.GrantReadUriPermission;
                        ContentResolver.TakePersistableUriPermission(uri, takeFlags);
                        PickFileTaskCompletionSource.TrySetResult(ReadFile(uri));
                        return;
                    }
                }
                catch { }
                PickFileTaskCompletionSource.TrySetResult(null);
            }
            else if (requestCode == PickFilesId)
            {
                try
                {
                    if (resultCode == Result.Ok)
                    {
                        var results = new List<PickFileResult>();
                        for (int i = 0; i < data.ClipData.ItemCount; i++)
                        {
                            var uri = data.ClipData.GetItemAt(i);
                            var takeFlags = ActivityFlags.GrantReadUriPermission;
                            ContentResolver.TakePersistableUriPermission(uri.Uri, takeFlags);
                            try
                            {
                                results.Add(ReadFile(uri.Uri));
                            }
                            catch { }
                        }
                        PickFilesTaskCompletionSource.TrySetResult(results);
                        return;
                    }
                }
                catch { }
                PickFilesTaskCompletionSource.TrySetResult(null);
            }
            else if (requestCode == PickFolderId)
            {
                if ((resultCode == Result.Ok) && (data != null))
                {
                    
                    Android.Net.Uri uri = data.Data;
                    var takeFlags = ActivityFlags.GrantReadUriPermission;
                    ContentResolver.TakePersistableUriPermission(uri, takeFlags);
                    PickFolderTaskCompletionSource.TrySetResult(new PickFileResult()
                    {
                        Uri = uri.ToString(),
                        Name = data.Data.Path,
                    });
                }
                else
                {
                    PickFolderTaskCompletionSource.TrySetResult(null);
                }
            }
            Finish();
        }
        PickFileResult ReadFile(Uri uri)
        {
            var documentFile = DocumentFile.FromSingleUri(this, uri);
            return new PickFileResult()
            {
                Name = documentFile.Name,
                Uri = uri.ToString(),
                Length = documentFile.Length(),
            };
        }
    }
}
