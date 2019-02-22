using System.Collections.ObjectModel;
using System.Net.Sockets;
using System.Threading.Tasks;
using App.Mobile.Remote.Utility;
using App.Mobile.Remote.Views;
using Xamarin.Forms;

namespace App.Mobile.Remote.ViewModels
{
	public class MainViewModel : ViewModelBase, IActivateable, INavigationAccess
	{
		private ObservableCollection<TextCommandViewModel> _options;

		public ObservableCollection<TextCommandViewModel> Options
		{
			get => _options ?? (_options = new ObservableCollection<TextCommandViewModel>());
			set => SetValue(ref _options, value, nameof(Options));
		}

		/// <inheritdoc />
		public Task ActivateAsync(bool activatedBefore)
		{
			if(activatedBefore)
				return Task.CompletedTask;

			Options.Add(new TextCommandViewModel("List agents", ListAgentsExecute ));

			return Task.CompletedTask;
		}

		private async void ListAgentsExecute(object obj)
		{
			await NavigationProxy.PushModalAsync(new AvailableRemoteAgents(), true);
		}

		/// <inheritdoc />
		public INavigation NavigationProxy { get; set; }
	}
}