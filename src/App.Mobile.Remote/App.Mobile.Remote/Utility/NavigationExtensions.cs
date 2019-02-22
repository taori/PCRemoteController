using System;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace App.Mobile.Remote.Utility
{
	public static class NavigationExtensions
	{
		public static async Task PushModelAsync(this INavigation source, Page page, object model, bool animated = true)
		{
			page.Behaviors.Add(new AppearingActivatorBehavior());
			page.BindingContext = model;
			await source.PushAsync(page, animated);
		}

		public static async Task PushModelModalAsync(this INavigation source, Page page, IDeactivateble model, bool animated = true)
		{
			TaskCompletionSource<object> tcs = new TaskCompletionSource<object>();

			page.Behaviors.Add(new AppearingActivatorBehavior());
			page.BindingContext = model;

			EventHandler<bool> modelOnDeativate = null;
			modelOnDeativate = delegate(object sender, bool b)
			{
				model.Deactivate -= modelOnDeativate;
				tcs.SetResult(null);
			};
			model.Deactivate += modelOnDeativate;

			await source.PushModalAsync(page, animated);
			await tcs.Task;
		}
	}
}