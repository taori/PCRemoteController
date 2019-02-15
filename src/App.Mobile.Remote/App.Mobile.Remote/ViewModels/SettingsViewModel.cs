using System.Threading.Tasks;
using System.Windows.Input;
using App.Mobile.Remote.Code;
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

		private int _bufferSize;

		public int BufferSize
		{
			get => _bufferSize;
			set => SetValue(ref _bufferSize, value, nameof(BufferSize));
		}

		private int _tcpPort;

		public int TcpPort
		{
			get => _tcpPort;
			set => SetValue(ref _tcpPort, value, nameof(TcpPort));
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
			ApplicationSettings.BufferSize = BufferSize;

			await ApplicationSettings.SaveAsync();
		}

		/// <inheritdoc />
		public Task ActivateAsync(bool activatedBefore)
		{
			UdpPort = ApplicationSettings.UdpPort;
			TcpPort = ApplicationSettings.TcpPort;
			BufferSize = ApplicationSettings.BufferSize;

			return Task.CompletedTask;
		}
	}
}