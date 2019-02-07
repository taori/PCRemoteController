using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Windows.Input;
using App.Mobile.Remote.Code;
using App.Mobile.Remote.Views;
using Xamarin.Forms;

namespace App.Mobile.Remote.ViewModels
{
	public class AvailableRemoteAgentsViewModel : ViewModelBase, IActivateable
	{
		private ObservableCollection<AgentViewModel> _agents = new ObservableCollection<AgentViewModel>();

		public ObservableCollection<AgentViewModel> Agents
		{
			get => _agents;
			set => SetValue(ref _agents, value, nameof(Agents));
		}

		private bool _anyAgentAvailable;

		public bool AnyAgentAvailable
		{
			get => _anyAgentAvailable;
			set => SetValue(ref _anyAgentAvailable, value, nameof(AnyAgentAvailable));
		}


		private UdpClient _beaconReceiver;

		private int _beaconPort;
		public int BeaconPort
		{
			get => _beaconPort;
			set => SetValue(ref _beaconPort, value, nameof(BeaconPort));
		}

		/// <inheritdoc />
		public async Task ActivateAsync(bool activatedBefore)
		{
			_beaconReceiver?.Dispose();
			_beaconReceiver = new UdpClient(BeaconPort);

			await Task.WhenAll(
				ListForBeaconAsync()
			);
		}

		private async Task ListForBeaconAsync()
		{
			await Task.Delay(2000);
			Agents.Clear();
			AnyAgentAvailable = Agents.Count > 0;
			Agents.Add(new AgentViewModel() { AgentName = "Agent1" });
			Agents.Add(new AgentViewModel() { AgentName = "Agent2" });
			AnyAgentAvailable = Agents.Count > 0;
		}
	}

	public class AgentViewModel : ViewModelBase
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
	}
}