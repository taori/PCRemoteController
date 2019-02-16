using System;
using System.Buffers;
using System.Collections.ObjectModel;
using System.IO.Pipelines;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reactive.Disposables;
using System.Reactive.Subjects;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using App.Mobile.Remote.Code;
using RemoteAgent.Common;
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

		private Subject<PipeAdapter> _whenPipeDone;
		public IObservable<PipeAdapter> WhenPipeDone => _whenPipeDone;

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
			_whenPipeDone = new Subject<PipeAdapter>();

			WhenTcpLineReceived.Subscribe(ReceivedContent);
			WhenPipeAvailable.Subscribe(OnPipeAvailable);
			WhenPipeDone.Subscribe(OnPipeDone);

			Task.Run(() => InteractWithTcpClientAsync(), _cts.Token);

			return Task.CompletedTask;
		}

		private void OnPipeDone(PipeAdapter pipe)
		{
			Log.Debug($"OnPipeDone");
			Log.Debug($"Unbinding events.");
			pipe.Received -= Received;
		}

		private async void OnPipeAvailable(PipeAdapter pipe)
		{
			Log.Debug($"{nameof(OnPipeAvailable)}.");
			var command = new HelloCommand("Android").ToBytes(ApplicationSettings.EncryptionPhrase);
			var sent = await pipe.Socket.SendAsync(new ArraySegment<byte>(command), SocketFlags.None);
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
			_whenPipeDone.OnNext(_pipeAdapter);
			_cts?.Cancel();
			_cts?.Dispose();
			_whenPipeDone?.Dispose();
			_whenPipeAvailable?.Dispose();
			_whenTcpLineReceived?.Dispose();
		}

		private CancellationTokenSource _cts = new CancellationTokenSource();

		private async Task InteractWithTcpClientAsync()
		{
			var serverEndpoint = new IPEndPoint(UdpEndpoint.Address, ApplicationSettings.TcpPort);

			var settings = new PipeAdapterSettings();
			settings.ExceptionHandler = e => Log.Error(e);
			if (_pipeAdapter != null)
				_whenPipeDone.OnNext(_pipeAdapter);

			Log.Debug($"Creating IO-Pipe.");
			var pipeAdapter = new PipeAdapter(_tcpClient.Client, settings);
			_pipeAdapter = pipeAdapter;
			pipeAdapter.Received += Received;

			Log.Debug($"Connecting to server [{serverEndpoint}].");
			await _tcpClient.ConnectAsync(serverEndpoint.Address, serverEndpoint.Port);

			_whenPipeAvailable.OnNext(pipeAdapter);
			while (!_cts.IsCancellationRequested && _tcpClient.Connected)
			{
				Log.Debug($"Processing input for [{serverEndpoint}].");
				await pipeAdapter.ExecuteAsync();
			}

			Log.Info("TCP Connection gone.");
		}

		private void Received(object sender, ReadOnlySequence<byte> e)
		{
			var line = e.ToString();
			_whenTcpLineReceived.OnNext(line);
		}
	}
}