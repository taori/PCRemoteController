using System;
using System.Buffers;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO.Pipelines;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using App.Mobile.Remote.DependencyInjection;
using App.Mobile.Remote.Utility;
using App.Mobile.Remote.Views;
using RemoteAgent.Common;
using RemoteAgent.Common.Commands;
using Toolkit.Cryptography;
using Toolkit.Pipelines;
using Xamarin.Forms;

namespace App.Mobile.Remote.ViewModels
{
	public class AgentViewModel : ViewModelBase, IActivateable, IDeactivateble, INavigationAccess, IDisposable
	{
		private static readonly NLog.ILogger Log = NLog.LogManager.GetLogger(nameof(AgentViewModel));

		private string _agentName;

		public string AgentName
		{
			get => _agentName;
			set => SetValue(ref _agentName, value, nameof(AgentName));
		}

		private IPEndPoint _remoteEndpoint;

		public IPEndPoint RemoteEndpoint
		{
			get => _remoteEndpoint;
			set => SetValue(ref _remoteEndpoint, value, nameof(RemoteEndpoint));
		}

		private DateTime _lastLifeSignal;

		public DateTime LastLifeSignal
		{
			get => _lastLifeSignal;
			set
			{
				if (SetValue(ref _lastLifeSignal, value, nameof(LastLifeSignal)))
					this.OnPropertyChanged(nameof(IsAlive));
			}
		}

		private Subject<RemoteCommand> _whenCommandTransmissionRequested = new Subject<RemoteCommand>();
		public IObservable<RemoteCommand> WhenCommandTransmissionRequested => _whenCommandTransmissionRequested;

		private ObservableCollection<TextCommandViewModel> _commands;

		public ObservableCollection<TextCommandViewModel> Commands
		{
			get => _commands ?? (_commands = new ObservableCollection<TextCommandViewModel>());
			set => SetValue(ref _commands, value, nameof(Commands));
		}

		public bool IsAlive => (DateTime.Now - LastLifeSignal).TotalSeconds < 20;

		private TcpClient _tcpClient;

		private CompositeDisposable _disposables;

		public AgentViewModel(IPEndPoint remoteEndpoint)
		{
			this.RemoteEndpoint = remoteEndpoint;
			BuildClientAsync();
		}

		private async void BuildClientAsync()
		{
			Log.Debug("Creating tcp client.");
			_tcpClient = new TcpClient();
			await ConnectClientAsync(_tcpClient);
		}

		/// <inheritdoc />
		public async Task ActivateAsync(bool activatedBefore)
		{
//			WhenTcpLineReceived.Subscribe(ReceivedContent);
//			WhenCommandTransmissionRequested.Subscribe(CommandTransmissionRequested);
			_disposables = new CompositeDisposable();
			_disposables.Add(WhenCommandTransmissionRequested.Subscribe(CommandTransmissionRequested));
			await SendCommandAsync(new ListCommandsCommand());
		}

		private async void CommandTransmissionRequested(RemoteCommand command)
		{
			var adapter = new PipeAdapter(_tcpClient.Client, CreatePipeAdapterSettings());
			var sent = await adapter.SendAsync(command.ToBytes(ApplicationSettings.EncryptionPhrase));
			Log.Info($"message bytes sent: [{sent}].");
		}

		private async Task SendCommandAsync(RemoteCommand command)
		{
			Log.Debug($"Sending command [{command.CommandName}].");
			_whenCommandTransmissionRequested.OnNext(command);
		}

		/// <inheritdoc />
		public async Task DeactivateAsync(bool activatedBefore)
		{
			Log.Debug($"{nameof(DeactivateAsync)}");
			_disposables.Dispose();
			Deactivate?.Invoke(this, activatedBefore);
		}

		/// <inheritdoc />
		public event EventHandler<bool> Deactivate;

		private async Task ConnectClientAsync(TcpClient client)
		{
			var settings = CreatePipeAdapterSettings();

			Log.Debug($"Creating IO-Pipe.");
			var pipeAdapter = new PipeAdapter(client.Client, settings);
			pipeAdapter.Settings.PipeSequenceChunkifier = new PipeSequenceChunkifier(Encoding.UTF8.GetBytes("\n"));
			pipeAdapter.Settings.ExceptionHandler = e => Log.Error(e);
			pipeAdapter.Received += Received;

			client.ReceiveBufferSize = 5 * 1024 * 1024;

			Log.Debug($"Connecting to server [{RemoteEndpoint}].");
			await client.ConnectAsync(RemoteEndpoint.Address, RemoteEndpoint.Port);
			Log.Debug($"Connection established [{client.Client.LocalEndPoint}] [{client.Client.RemoteEndPoint}].");

			if (client.Connected)
			{
				Log.Debug($"Processing input for [{RemoteEndpoint}].");
				await pipeAdapter.ListenAsync();
			}

			Log.Info("TCP Connection gone.");
		}

		private static PipeAdapterSettings CreatePipeAdapterSettings()
		{
			var settings = new PipeAdapterSettings();
			settings.ExceptionHandler = e => Log.Error(e);
			return settings;
		}

		private async void Received(object sender, byte[] data)
		{
			Log.Debug($"Receiving content from server.");

			var receivedContent = Encoding.UTF8.GetString(data);
			Log.Debug(receivedContent);
			var command = RemoteCommandFactory.FromBytes(data);
			if (command != null)
			{
				Log.Info($"Executing command [{command.CommandName}].");
				Log.Debug($"Converting command to concrete type.");
				await ProcessCommandAsync(command);
			}
		}

		private Task ProcessCommandAsync(RemoteCommand command)
		{
			switch (command)
			{
				case HelloCommand helloCommand:
					Log.Info($"Hello [{helloCommand.Who}]!");
					break;
				case ListCommandsResponseCommand commandsList:
					LoadCommands(commandsList);
					break;
				case DisplayMessageCommand messageCommand:
					var toastService = DependencyService.Get<IToastService>();
					toastService.DisplayToast(messageCommand.Message);
					break;
				default:
					Log.Warn($"Command [{command.CommandName}] is not handled.");
					break;
			}

			return Task.CompletedTask;
		}

		private void LoadCommands(ListCommandsResponseCommand commandsList)
		{
			Commands.Clear();
			var commands = commandsList.Commands.Select(d => ConvertToCommand(d));
			Commands = new ObservableCollection<TextCommandViewModel>(commands);
		}

		private TextCommandViewModel ConvertToCommand(RemoteCommand remoteCommand)
		{
			return new TextCommandViewModel(remoteCommand.CommandName, async (o) =>
			{
				if (UpdateCommandViewModel.CanHandle(remoteCommand))
					await this.NavigationProxy.PushModelModalAsync(new UpdateCommandModelPage(), new UpdateCommandViewModel(remoteCommand));
				await SendCommandAsync(remoteCommand);
			});
		}

		/// <inheritdoc />
		public INavigation NavigationProxy { get; set; }

		/// <inheritdoc />
		public void Dispose()
		{
			_whenCommandTransmissionRequested?.Dispose();
			_tcpClient?.Dispose();
			_disposables?.Dispose();
		}
	}
}