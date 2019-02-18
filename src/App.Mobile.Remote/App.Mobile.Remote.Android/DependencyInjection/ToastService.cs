using System;
using System.Runtime.CompilerServices;
using Android.OS;
using Android.Widget;
using App.Mobile.Remote.DependencyInjection;
using App.Mobile.Remote.Droid.DependencyInjection;

[assembly: Xamarin.Forms.Dependency(typeof(ToastService))]
namespace App.Mobile.Remote.Droid.DependencyInjection
{
	public class ToastService : IToastService
	{
		/// <inheritdoc />
		public void DisplayToast(string message, ToastDuration duration = ToastDuration.Short)
		{
			using (var handler = new Handler(Looper.MainLooper))
			{
				handler.Post(() =>
				{
					switch (duration)
					{
						case ToastDuration.Short:
							Toast.MakeText(Android.App.Application.Context, message, ToastLength.Short).Show();
							break;
						case ToastDuration.Long:
							Toast.MakeText(Android.App.Application.Context, message, ToastLength.Long).Show();
							break;
					}
				});
			}
		}
	}
}