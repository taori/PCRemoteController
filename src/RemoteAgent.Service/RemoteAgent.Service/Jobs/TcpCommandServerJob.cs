using System;
using System.Buffers;
using System.Configuration;
using System.IO.Pipelines;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using RemoteAgent.Common;
using RemoteAgent.Common.Commands;
using RemoteAgent.Service.CommandHandling;
using RemoteAgent.Service.Shell;
using Toolkit.Pipelines;

namespace RemoteAgent.Service.Jobs
{
	public class TcpCommandServerJob : JobBase
	{
		private Thread _thread;
		private TcpListener _tcpListener;
		private CancellationToken _cancellation;
		
		private bool _aborting;

		private void AbortThread()
		{
			if (_aborting)
				return;

			_aborting = true;
			_thread.Interrupt();
			_thread.Join(5000);
			_thread.Abort();
		}

		/// <inheritdoc />
		public override void OnShutdown()
		{
			AbortThread();
		}

		/// <inheritdoc />
		public override void Dispose(bool disposing)
		{
			AbortThread();
		}

		/// <inheritdoc />
		public override async Task WorkAsync(string[] args, CancellationToken cancellationToken)
		{
			_cancellation = cancellationToken;
			_thread = new Thread(DoWork);
			_thread.IsBackground = true;
			_thread.Start();
		}

		private async void DoWork(object cancellationToken)
		{
			try
			{
				var port = ConfigurationManager.AppSettings["TcpListenerPort"] ?? "8087";
				if (!int.TryParse(port, out var parsedPort))
				{
					Logger.Error($"TcpListenerPort [{port}] is not a valid value.");
					return;
				}

				var backlogSize = ConfigurationManager.AppSettings["TcpBackLogSize"];
				if (!int.TryParse(backlogSize, out var parsedBacklogsize))
				{
					Logger.Error($"TcpBackLogSize [{backlogSize}] is not a valid value.");
					return;
				}

				var localIp = Dns.GetHostAddresses(Dns.GetHostName()).FirstOrDefault(d => d.AddressFamily == AddressFamily.InterNetwork);
				Logger.Info($"Binding TcpListener to [{localIp}].");
				_tcpListener = new TcpListener(new IPEndPoint(localIp, parsedPort));
				Logger.Info($"Starting listener with backlog [{parsedBacklogsize}].");
				_tcpListener.Start(parsedBacklogsize);

				while (!_cancellation.IsCancellationRequested)
				{
					Logger.Debug($"Waiting for client on [{parsedPort}].");
					var remoteSocket = await _tcpListener.AcceptSocketAsync();
					Logger.Info($"Client connected [{remoteSocket.RemoteEndPoint}].");
					HandleSocketAsync(remoteSocket, _cancellation);
				}
			}
			catch (Exception e)
			{
				Logger.Fatal($"[{nameof(TcpCommandServerJob)}] crashed.");
				Logger.Fatal(e);
			}
			finally
			{
				Logger.Info($"[{nameof(TcpCommandServerJob)}] Shutting down listener.");
				_tcpListener?.Stop();
			}
		}

		private async void HandleSocketAsync(Socket socket, CancellationToken cancellationToken)
		{
			try
			{
				var adapter = new PipeAdapter(socket);
				adapter.Settings.PipeSequenceChunkifier = new PipeSequenceChunkifier(Encoding.UTF8.GetBytes("\n"));

				adapter.Settings.ExceptionHandler = e => Logger.Error(e);
				adapter.Received += AdapterOnReceived;

				if (socket.Connected)
					await adapter.ListenAsync();
			}
			catch (OperationCanceledException)
			{
			}
			catch (Exception e)
			{
				Logger.Error(e);
			}
			finally
			{
				if (socket.Connected)
					socket.Disconnect(true);
				Logger.Info($"Client disconnected [{socket.RemoteEndPoint}].");
			}
		}

		private async void AdapterOnReceived(object sender, byte[] data)
		{
			var command = RemoteCommandFactory.FromBytes(data);
			if (command != null)
			{
				Logger.Info($"Executing command [{command.CommandName}].");
				Logger.Debug($"Converting command to concrete type.");
				await ProcessCommandAsync(command, sender as PipeAdapter);
			}
		}
		
		private async Task ProcessCommandAsync(RemoteCommand command, PipeAdapter adapter)
		{
			try
			{
				await CommandHandler.HandleAsync(command, adapter.Socket);
				await Task.Delay(100);
				await CommandHandler.ExecuteCommandAsync(new DisplayMessageCommand($"Command \"{command.CommandName}\" executed."), adapter.Socket);
			}
			catch (Exception e)
			{
				Logger.Error(e);
			}
		}
	}
}