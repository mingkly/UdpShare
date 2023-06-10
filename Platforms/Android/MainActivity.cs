using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Net;
using Android.OS;
using Android.Provider;
using AndroidX.DocumentFile.Provider;

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
    }
    public static string GetAbsolutePath(string path)
    {
        var uri=Android.Net.Uri.Parse(path);
        System.Diagnostics.Debug.WriteLine(uri.Path);
        uri = MediaStore.GetMediaUri(Android.App.Application.Context, uri);
        using var cusor = Android.App.Application.Context.ContentResolver?.Query(uri,
            new string[] { "_data",MediaStore.IMediaColumns.RelativePath}, null, null, null);
        if (cusor != null && cusor.MoveToNext())
        {
            var dataCol = cusor.GetColumnIndex("_data");
            return cusor.GetString(dataCol);
        }
        return uri.Path;
    }

}
