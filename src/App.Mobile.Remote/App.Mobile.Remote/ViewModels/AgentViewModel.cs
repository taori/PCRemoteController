using System;
using System.Buffers;
using System.Collections.ObjectModel;
using System.IO.Pipelines;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reactive.Disposables;
using System.Reactive.Subjects;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using App.Mobile.Remote.Code;
using App.Mobile.Remote.DependencyInjection;
using RemoteAgent.Common;
using RemoteAgent.Common.Commands;
using Toolkit.Cryptography;
using Toolkit.Pipelines;
using Xamarin.Forms;
using Application = Xamarin.Forms.PlatformConfiguration.AndroidSpecific.AppCompat.Application;

namespace App.Mobile.Remote.ViewModels
{
	public class AgentViewModel : ViewModelBase, IActivateable, IDeactivateble
	{
		private static readonly NLog.ILogger Log = NLog.LogManager.GetLogger(nameof(AgentViewModel));

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

		private Subject<string> _whenTcpLineReceived;
		public IObservable<string> WhenTcpLineReceived => _whenTcpLineReceived;

		private Subject<PipeAdapter> _whenPipeAvailable;
		public IObservable<PipeAdapter> WhenPipeAvailable => _whenPipeAvailable;

		private ObservableCollection<TextCommandViewModel> _commands;

		public ObservableCollection<TextCommandViewModel> Commands
		{
			get => _commands ?? (_commands = new ObservableCollection<TextCommandViewModel>());
			set => SetValue(ref _commands, value, nameof(Commands));
		}
		
		public bool IsAlive => (DateTime.Now - LastLifeSignal).TotalSeconds < 20;

		private PipeAdapter _pipeAdapter;

		private TcpClient _tcpClient;

		private CompositeDisposable _disposables = new CompositeDisposable();

		/// <inheritdoc />
		public Task ActivateAsync(bool activatedBefore)
		{
			Log.Debug("Creating tcp client.");
			_tcpClient = new TcpClient();

			Log.Debug("Creating cancellation token.");
			_cts = new CancellationTokenSource();

			_whenTcpLineReceived = new Subject<string>();
			_whenPipeAvailable = new Subject<PipeAdapter>();

			WhenTcpLineReceived.Subscribe(ReceivedContent);
			WhenPipeAvailable.Subscribe(OnPipeAvailable);

			Task.Run(() => InteractWithTcpClientAsync(), _cts.Token);

			return Task.CompletedTask;
		}

		private async void OnPipeAvailable(PipeAdapter pipe)
		{
			Log.Debug($"{nameof(OnPipeAvailable)}.");
			await Task.Delay(100);
			await SendCommandAsync(new HelloCommand("Android"));
			await SendCommandAsync(new ListCommandsCommand());
		}

		private async Task SendCommandAsync(RemoteCommand command)
		{
			Log.Debug($"Sending command [{command.CommandName}].");
			var bytes = command.ToBytes(ApplicationSettings.EncryptionPhrase, ApplicationSettings.CommandDelimiter);
			var sent = await _tcpClient.Client.SendAsync(new ArraySegment<byte>(bytes), SocketFlags.None);
			Log.Info($"message bytes sent: [{sent}].");
		}

		private void ReceivedContent(string content)
		{
			Commands.Add(new TextCommandViewModel(content, Command));
			Log.Info($"Received command: {content}");
		}

		private void Command(object obj)
		{
			throw new NotImplementedException();
		}

		/// <inheritdoc />
		public async Task DeactivateAsync(bool activatedBefore)
		{
			Log.Debug($"{nameof(DeactivateAsync)}");
			_tcpClient?.Dispose();
			_cts?.Cancel();
			_cts?.Dispose();
			_whenPipeAvailable?.Dispose();
			_whenTcpLineReceived?.Dispose();
		}

		private CancellationTokenSource _cts = new CancellationTokenSource();

		private async Task InteractWithTcpClientAsync()
		{
			var serverEndpoint = new IPEndPoint(UdpEndpoint.Address, ApplicationSettings.TcpPort);

			var settings = new PipeAdapterSettings();
			settings.ExceptionHandler = e => Log.Error(e);

			Log.Debug($"Creating IO-Pipe.");
			var pipeAdapter = new PipeAdapter(_tcpClient.Client, settings);
			pipeAdapter.Settings.PipeSequenceChunkifier = new PipeSequenceChunkifier(Encoding.UTF8.GetBytes(ApplicationSettings.CommandDelimiter), (byte)'\\');
			pipeAdapter.Settings.ExceptionHandler = e => Log.Error(e);
			pipeAdapter.Received += Received;
			_pipeAdapter = pipeAdapter;

			Log.Debug($"Connecting to server [{serverEndpoint}].");
			await _tcpClient.ConnectAsync(serverEndpoint.Address, serverEndpoint.Port);
			Log.Debug($"Connection established [{_tcpClient.Client.LocalEndPoint}] [{_tcpClient.Client.RemoteEndPoint}].");

			_whenPipeAvailable.OnNext(pipeAdapter);
			while (!_cts.IsCancellationRequested && _tcpClient.Connected)
			{
				Log.Debug($"Processing input for [{serverEndpoint}].");
				await Task.Delay(1);
				await pipeAdapter.ExecuteAsync();
			}

			Log.Info("TCP Connection gone.");
		}

		private async void Received(object sender, ReadOnlySequence<byte> e)
		{
			var converted = e.ToArray();
			var receivedContent = Encoding.UTF8.GetString(converted);
			Log.Debug(receivedContent);
			var command = RemoteCommandFactory.FromBytes(converted);
			if (command != null)
			{
				Log.Info($"Executing command [{command.CommandName}].");
				Log.Debug($"Converting command to concrete type.");
				await ProcessCommandAsync(command);
			}
		}

		private Task ProcessCommandAsync(RemoteCommand command)
		{
//			var concrete = RemoteCommandFactory.FromCommand(command);
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
			return new TextCommandViewModel(remoteCommand.CommandName, async (o) => await SendCommandAsync(remoteCommand));
		}
	}
}