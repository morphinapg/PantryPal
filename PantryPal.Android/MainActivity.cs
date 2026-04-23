using Android;
using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Runtime;
using AndroidX.Core.App;
using AndroidX.Core.Content;
using Avalonia;
using Avalonia.Android;
using Java.Security;
using System;
using System.Security.Permissions;

namespace PantryPal.Android;

[Activity(
    Label = "PantryPal",
    Theme = "@style/MyTheme.NoActionBar",
    Icon = "@drawable/icon",
    MainLauncher = true,
    ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.UiMode)]
public class MainActivity : AvaloniaMainActivity
{

    //protected override AppBuilder CustomizeAppBuilder(AppBuilder builder)
    //{
    //    return base.CustomizeAppBuilder(builder)
    //        .WithInterFont();
    //}
}

[Application]
public class AndroidApp : AvaloniaAndroidApplication<App>
{
    protected AndroidApp(IntPtr javaReference, JniHandleOwnership transfer)
       : base(javaReference, transfer)
    {
    }
}
