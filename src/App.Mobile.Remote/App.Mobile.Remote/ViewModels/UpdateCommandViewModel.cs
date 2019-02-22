using System;
using System.Threading.Tasks;
using App.Mobile.Remote.Utility;
using RemoteAgent.Common.Commands;
using Xamarin.Forms;

namespace App.Mobile.Remote.ViewModels
{
	public class UpdateCommandViewModel : ViewModelBase, IDeactivateble, IActivateable, INavigationAccess
	{
		private readonly RemoteCommand _remoteCommand;

		public UpdateCommandViewModel(RemoteCommand remoteCommand)
		{
			_remoteCommand = remoteCommand;
		}

		/// <inheritdoc />
		public async Task DeactivateAsync(bool activatedBefore)
		{
			await Xamarin.Forms.Application.Current.MainPage.DisplayAlert("Deactivating", "Deactivating", "OK");
			Deactivate?.Invoke(this, activatedBefore);
		}

		/// <inheritdoc />
		public event EventHandler<bool> Deactivate;

		/// <inheritdoc />
		public async Task ActivateAsync(bool activatedBefore)
		{
			if (!CanHandle(_remoteCommand))
			{
				await NavigationProxy.PopModalAsync();
			}
		}

		public static bool CanHandle(RemoteCommand command)
		{
			return false;
		}

		/// <inheritdoc />
		public INavigation NavigationProxy { get; set; }
	}
}