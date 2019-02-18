using System;
using System.Buffers;
using System.Configuration;
using System.IO;
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
using RemoteAgent.Service.Shell;
using Toolkit.Pipelines;

namespace RemoteAgent.Service.Jobs
{
	public class TcpCommandServerJob : JobBase
	{
		/// <inheritdoc />
		public override void OnShutdown()
		{
		}

		/// <inheritdoc />
		public override void Dispose(bool disposing)
		{
		}

		/// <inheritdoc />
		public override async Task WorkAsync(string[] args, CancellationToken cancellationToken)
		{
			TcpListener listener = null;
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
				listener = new TcpListener(new IPEndPoint(localIp, parsedPort));
				Logger.Info($"Starting listener with backlog [{parsedBacklogsize}].");
				listener.Start(parsedBacklogsize);

				while (!cancellationToken.IsCancellationRequested)
				{
					Logger.Debug($"Waiting for client on [{parsedPort}].");
					var remoteSocket = await listener.AcceptSocketAsync();
					Logger.Info($"Client connected [{remoteSocket.RemoteEndPoint}].");
					HandleSocketAsync(remoteSocket, cancellationToken);
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
				listener?.Stop();
			}
		}

		private async void HandleSocketAsync(Socket socket, CancellationToken cancellationToken)
		{
			try
			{
				var delimiter = ConfigurationManager.AppSettings["CommandDelimiter"];
				Logger.Info($"Delimiter [{delimiter}] is used to delimit commands.");
				var adapter = new PipeAdapter(socket);
				adapter.Settings.PipeSequenceChunkifier = new PipeSequenceChunkifier(Encoding.UTF8.GetBytes(delimiter), (byte)'\\');
				adapter.Settings.ExceptionHandler = e => Logger.Error(e);
				adapter.Received += AdapterOnReceived;
				while (!cancellationToken.IsCancellationRequested && socket.Connected)
				{
					await Task.Delay(1);
					await adapter.ExecuteAsync();
				}

				Logger.Info($"Disconnecting client [{socket.RemoteEndPoint}].");
				socket.Disconnect(true);
				Logger.Info($"Client disconnected [{socket.RemoteEndPoint}].");
			}
			catch (OperationCanceledException) { }
			catch (Exception e)
			{
				Logger.Error(e);
			}
		}

		private async void AdapterOnReceived(object sender, ReadOnlySequence<byte> e)
		{
			var converted = e.ToArray();
			var command = RemoteCommandFactory.FromBytes(converted);
			if (command != null)
			{
				Logger.Info($"Executing command [{command.CommandName}].");
				Logger.Debug($"Converting command to concrete type.");
				await ProcessCommandAsync(command, sender as PipeAdapter);
			}
		}

		private async Task ProcessCommandAsync(RemoteCommand command, PipeAdapter adapter)
		{
//			var concrete = RemoteCommandFactory.FromCommand(command);

			switch (command)
			{
				case HelloCommand helloCommand:
					Logger.Info($"Hello [{helloCommand.Who}]!");
					break;
				case ListCommandsCommand listCommandsCommand:
					var responseCommand = new ListCommandsResponseCommand(new []
					{
						new HelloCommand("Server"), 
					});
					await ExecuteCommandAsync(responseCommand, adapter);
					await ExecuteCommandAsync(new DisplayMessageCommand("List loaded."), adapter);
					break;
				default:
					Logger.Warn($"Command [{command.CommandName}] is not handled.");
					break;
			}
		}

		private async Task ExecuteCommandAsync(RemoteCommand remoteCommand, PipeAdapter adapter)
		{
			var encryptionKey = ConfigurationManager.AppSettings["EncryptionPhrase"];
			var commandDelimiter = ConfigurationManager.AppSettings["CommandDelimiter"];
			Logger.Info($"Sending command [{remoteCommand.CommandName}] to [{adapter.Socket.RemoteEndPoint}].");
			var message = remoteCommand.ToBytes(encryptionKey, commandDelimiter);
//			var sent = await adapter.Socket.SendAsync(new ArraySegment<byte>(message), SocketFlags.None);
			var sent = await adapter.Socket.SendToAsync(new ArraySegment<byte>(message), SocketFlags.None, adapter.Socket.RemoteEndPoint);
			Logger.Debug($"Sent [{sent}] bytes to [{adapter.Socket.RemoteEndPoint}].");
		}
	}
}