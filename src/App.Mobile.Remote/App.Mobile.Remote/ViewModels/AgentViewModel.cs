using System;
using System.Buffers;
using System.Collections.ObjectModel;
using System.IO.Pipelines;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reactive.Subjects;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using App.Mobile.Remote.Code;
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

		private Subject<string> _whenTcpLineReceived = new Subject<string>();
		public IObservable<string> WhenTcpLineReceived => _whenTcpLineReceived;

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
			if(activatedBefore)
				return Task.CompletedTask;

			_cts = new CancellationTokenSource();

			WhenTcpLineReceived.Subscribe(ReceivedContent);
			Task.Run(() => InteractWithTcpClientAsync(), _cts.Token);

			return Task.CompletedTask;
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
			_cts?.Cancel();
			_cts?.Dispose();
		}

		private CancellationTokenSource _cts = new CancellationTokenSource();

		private IPAddress GetLocalIpAddress()
		{
			var hostAddresses = Dns.GetHostAddresses(Dns.GetHostName());
			var filter = hostAddresses.FirstOrDefault(d => d.AddressFamily == AddressFamily.InterNetwork || d.AddressFamily == AddressFamily.InterNetworkV6);
			return filter;
		}

		private async Task InteractWithTcpClientAsync()
		{
			using (var udpClient = new UdpClient())
			{
				var message = "Retrieving commands";
				await udpClient.SendAsync(Encoding.UTF8.GetBytes(message), message.Length, new IPEndPoint(IPAddress.Broadcast, ApplicationSettings.UdpPort));
			}

			using (var socket = new Socket(SocketType.Stream, ProtocolType.Tcp))
			{
//				var localIpAddress = GetLocalIpAddress();
				await socket.ConnectAsync(new IPEndPoint(UdpEndpoint.Address, ApplicationSettings.TcpPort));
//				socket.Bind(new IPEndPoint(IPAddress.Any, ApplicationSettings.TcpPort));
//				socket.Listen(120);
//				var remoteSocket = await socket.AcceptAsync();

				while (!_cts.IsCancellationRequested)
				{
					await ProcessLinesAsync(socket);
				}
			}
		}

		private async Task ProcessLinesAsync(Socket socket)
		{
			var pipe = new Pipe();
			var writing = FillPipeAsync(socket, pipe.Writer);
			var reading = ReadPipeAsync(pipe.Reader);

			await Task.WhenAll(reading, writing);
		}

		private async Task FillPipeAsync(Socket socket, PipeWriter writer)
		{
			while (true)
			{
				var memory = writer.GetMemory(ApplicationSettings.UdpPort);
				try
				{
					var bytesRead = await socket.ReceiveAsync(memory, SocketFlags.None);
					if (bytesRead == 0)
					{
						break;
					}
					writer.Advance(bytesRead);
				}
				catch (Exception ex)
				{
					Log.Error(ex);
					break;
				}

				var result = await writer.FlushAsync();
				if (result.IsCompleted)
				{
					break;
				}
			}

			writer.Complete();
		}

		private async Task ReadPipeAsync(PipeReader reader)
		{
			while (true)
			{
				var result = await reader.ReadAsync();
				var buffer = result.Buffer;
				SequencePosition? position = null;

				do
				{
					position = buffer.PositionOf((byte)'\n');

					if (position != null)
					{
						await ProcessLineAsync(buffer.Slice(0, position.Value));

						buffer = buffer.Slice(buffer.GetPosition(1, position.Value));
					}
				}
				while (position != null);

				reader.AdvanceTo(buffer.Start, buffer.End);

				if (result.IsCompleted)
				{
					break;
				}
			}

			reader.Complete();
		}

		private async Task ProcessLineAsync(ReadOnlySequence<byte> slice)
		{
			var line = slice.ToString();
			_whenTcpLineReceived.OnNext(line);
		}
	}
}