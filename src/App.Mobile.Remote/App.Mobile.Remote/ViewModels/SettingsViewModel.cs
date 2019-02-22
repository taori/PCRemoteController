using System.Threading.Tasks;
using System.Windows.Input;
using App.Mobile.Remote.Utility;
using Xamarin.Forms;

namespace App.Mobile.Remote.ViewModels
{
	public class SettingsViewModel : ViewModelBase, IActivateable
	{
		private int _udpPort;

		public int UdpPort
		{
			get => _udpPort;
			set => SetValue(ref _udpPort, value, nameof(UdpPort));
		}

		private int _tcpPort;

		public int TcpPort
		{
			get => _tcpPort;
			set => SetValue(ref _tcpPort, value, nameof(TcpPort));
		}

		private string _encryptionPhrase;

		public string EncryptionPhrase
		{
			get => _encryptionPhrase;
			set => SetValue(ref _encryptionPhrase, value, nameof(EncryptionPhrase));
		}

		private ICommand _saveCommand;

		public ICommand SaveCommand
		{
			get => _saveCommand ?? (_saveCommand = new Command(SaveExecute));
			set => SetValue(ref _saveCommand, value, nameof(SaveCommand));
		}

		private async void SaveExecute(object obj)
		{
			ApplicationSettings.UdpPort = UdpPort;
			ApplicationSettings.TcpPort = TcpPort;
			ApplicationSettings.EncryptionPhrase = EncryptionPhrase;

			await ApplicationSettings.SaveAsync();
		}

		/// <inheritdoc />
		public Task ActivateAsync(bool activatedBefore)
		{
			UdpPort = ApplicationSettings.UdpPort;
			TcpPort = ApplicationSettings.TcpPort;
			EncryptionPhrase = ApplicationSettings.EncryptionPhrase;

			return Task.CompletedTask;
		}
	}
}