using System;
using System.Collections.ObjectModel;
using System.Net;
using System.Threading.Tasks;
using System.Windows.Input;
using App.Mobile.Remote.Code;
using Xamarin.Forms;

namespace App.Mobile.Remote.ViewModels
{
	public class AgentViewModel : ViewModelBase, IActivateable
	{
		private ICommand _selectCommand;

		public ICommand SelectCommand
		{
			get => _selectCommand ?? (_selectCommand = new Command(SelectExecute));
			set => SetValue(ref _selectCommand, value, nameof(SelectCommand));
		}

		private void SelectExecute()
		{
		}

		private string _agentName;

		public string AgentName
		{
			get => _agentName;
			set => SetValue(ref _agentName, value, nameof(AgentName));
		}

		private IPEndPoint _udpEndpoint;

		public IPEndPoint UdpEndpoint
		{
			get => _udpEndpoint;
			set => SetValue(ref _udpEndpoint, value, nameof(UdpEndpoint));
		}

		private DateTime _lastLifeSignal;

		public DateTime LastLifeSignal
		{
			get => _lastLifeSignal;
			set
			{
				if(SetValue(ref _lastLifeSignal, value, nameof(LastLifeSignal))) 
					this.OnPropertyChanged(nameof(IsAlive));
			}
		}

		private ObservableCollection<TextCommandViewModel> _commands;

		public ObservableCollection<TextCommandViewModel> Commands
		{
			get => _commands ?? (_commands = new ObservableCollection<TextCommandViewModel>());
			set => SetValue(ref _commands, value, nameof(Commands));
		}
		
		public bool IsAlive => (DateTime.Now - LastLifeSignal).TotalSeconds < 20;

		/// <inheritdoc />
		public Task ActivateAsync(bool activatedBefore)
		{
			return Task.CompletedTask;
		}
	}
}