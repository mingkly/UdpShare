using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Net;
using Android.OS;
using Android.Provider;
using AndroidX.DocumentFile.Provider;
using UdpQuickShare.FileActions.FilePickers;
using UdpQuickShare.FileActions.FileSavers;
using UdpQuickShare.Services;
using Uri = Android.Net.Uri;

namespace UdpQuickShare;

[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
    public static MainActivity Instance { get; private set; }
    protected override void OnCreate(Bundle savedInstanceState)
    {
        base.OnCreate(savedInstanceState);;
        Instance = this;
        AndroidX.Fragment.App.FragmentTransaction t;
        

    }
    public TaskCompletionSource<PickFileResult> PickFileTaskCompletionSource { get; set; }
    public int PickFileId = 1;
    public TaskCompletionSource<IEnumerable<PickFileResult>> PickFilesTaskCompletionSource { get; set; }
    public int PickFilesId = 2;
    public TaskCompletionSource<PickFileResult> PickFolderTaskCompletionSource { get; set; }
    public int PickFolderId = 3;

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
        else if(requestCode == PickFilesId)
        {
            try
            {
                if (resultCode == Result.Ok)
                {
                    var results = new List<PickFileResult>();
                    for(int i = 0; i < data.ClipData.ItemCount; i++)
                    {
                        var uri=data.ClipData.GetItemAt(i);
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
                    Uri=uri.ToString(),
                    Name=data.Data.Path,
                });
            }
            else
            {
                PickFolderTaskCompletionSource.TrySetResult(null);
            }
        }
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
