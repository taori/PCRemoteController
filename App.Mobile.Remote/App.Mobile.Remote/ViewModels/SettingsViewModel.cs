using System.Threading.Tasks;
using System.Windows.Input;
using App.Mobile.Remote.Code;
using Xamarin.Forms;

namespace App.Mobile.Remote.ViewModels
{
	public class SettingsViewModel : ViewModelBase, IActivateable
	{
		private int _beaconPort;

		public int BeaconPort
		{
			get => _beaconPort;
			set => SetValue(ref _beaconPort, value, nameof(BeaconPort));
		}

		private ICommand _saveCommand;

		public ICommand SaveCommand
		{
			get => _saveCommand ?? (_saveCommand = new Command(SaveExecute));
			set => SetValue(ref _saveCommand, value, nameof(SaveCommand));
		}

		private async void SaveExecute(object obj)
		{
			ApplicationSettings.BeaconPort = BeaconPort;
			await ApplicationSettings.SaveAsync();
		}

		/// <inheritdoc />
		public Task ActivateAsync(bool activatedBefore)
		{
			BeaconPort = ApplicationSettings.BeaconPort;

			return Task.CompletedTask;
		}
	}
}