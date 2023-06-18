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
    static PowerManager.WakeLock wakeLock;
    public  static void WakeCpu()
    {
        if(wakeLock == null)
        {
            var context = Android.App.Application.Context;
            PowerManager powerManager = (PowerManager)context.GetSystemService(Context.PowerService);
            wakeLock = powerManager.NewWakeLock(WakeLockFlags.Partial, "Download");
        }
        wakeLock.Acquire();
    }
    public static void SleepCpu()
    {
        wakeLock?.Release();
        wakeLock?.Dispose();
        wakeLock = null;
    }
}
