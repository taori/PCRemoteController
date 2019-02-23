using System;

using Android.App;
using Android.Content.PM;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;
using NLog;
using NLog.Config;
using NLog.Targets;

namespace App.Mobile.Remote.Droid
{
    [Activity(Label = "PC Remote", Icon = "@mipmap/icon", Theme = "@style/MainTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation)]
    public class MainActivity : global::Xamarin.Forms.Platform.Android.FormsAppCompatActivity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            TabLayoutResource = Resource.Layout.Tabbar;
            ToolbarResource = Resource.Layout.Toolbar;

			var configuration = new LoggingConfiguration();
			configuration.AddTarget("debugger", new OutputDebugStringTarget());
			configuration.AddRule(LogLevel.Trace, LogLevel.Fatal, "debugger");
			NLog.LogManager.Configuration = configuration;


			base.OnCreate(savedInstanceState);
            global::Xamarin.Forms.Forms.SetFlags("Shell_Experimental");
			global::Xamarin.Forms.Forms.Init(this, savedInstanceState);
            LoadApplication(new App());
        }
    }
}