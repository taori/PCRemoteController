using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Input;
using App.Mobile.Remote.Utility;
using App.Mobile.Remote.Views;
using Xamarin.Forms;

namespace App.Mobile.Remote.ViewModels
{
	public class AvailableRemoteAgentsViewModel : ViewModelBase, IActivateable, INavigationAccess
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

		private string _title;

		public string Title
		{
			get => _title;
			set => SetValue(ref _title, value, nameof(Title));
		}

		private UdpClient _beaconReceiver;

		private Subject<UdpReceiveResult> _whenMessageReceived = new Subject<UdpReceiveResult>();
		public IObservable<UdpReceiveResult> WhenMessageReceived => _whenMessageReceived;

		/// <inheritdoc />
		public async Task ActivateAsync(bool activatedBefore)
		{
			Title = $"Agents connected to UDP port {ApplicationSettings.UdpPort}.";
			_beaconReceiver?.Dispose();
			if (activatedBefore)
				return;

			WhenMessageReceived.Subscribe(MessageReceivedCallback);
			var ep = new IPEndPoint(IPAddress.Any, ApplicationSettings.UdpPort);
			_beaconReceiver = new UdpClient(ep);

			await Task.WhenAny(
				ListForBeaconAsync(),
				Task.Delay(100)
			);
		}

		private static readonly Regex UdpReceivePattern = new Regex(@"\[(?<agent>[^]]+)]");

		private void MessageReceivedCallback(UdpReceiveResult udpReceive)
		{
			var remoteTcpPort = new IPEndPoint(udpReceive.RemoteEndPoint.Address, ApplicationSettings.TcpPort);
			var agentMatch = Agents.FirstOrDefault(d => d.RemoteEndpoint.Equals(remoteTcpPort));
			if (agentMatch == null)
			{
				var message = Encoding.UTF8.GetString(udpReceive.Buffer);
				if (!UdpReceivePattern.IsMatch(message))
					return;

				var name = UdpReceivePattern.Match(message).Groups["agent"].Value;
				Agents.Add(new AgentViewModel(remoteTcpPort)
				{
					AgentName = $"{name} @ {remoteTcpPort}",
					LastLifeSignal = DateTime.Now
				});
			}
			else
			{
				agentMatch.LastLifeSignal = DateTime.Now;
			}
		}

		private ICommand _selectCommand;

		public ICommand SelectCommand
		{
			get => _selectCommand ?? (_selectCommand = new Command(OpenAgentExecute));
			set => SetValue(ref _selectCommand, value, nameof(SelectCommand));
		}

		private async void OpenAgentExecute(object obj)
		{
			var page = new CommandAgent();
			if (obj is AgentViewModel agent)
				page.BindingContext = agent;

			await NavigationProxy.PushModalAsync(page);
		}

		private Task ListForBeaconAsync()
		{
			return Task.Run(async () =>
			{
				while (true)
				{
					var message = await _beaconReceiver.ReceiveAsync();
					_whenMessageReceived.OnNext(message);
				}
			});
		}

		/// <inheritdoc />
		public INavigation NavigationProxy { get; set; }
	}
}