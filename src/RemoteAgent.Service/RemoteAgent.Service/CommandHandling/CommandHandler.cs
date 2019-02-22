using System;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32;
using NLog;
using RemoteAgent.Common.Commands;
using RemoteAgent.Service.Jobs;
using RemoteAgent.Service.Utility;
using Toolkit.Pipelines;

namespace RemoteAgent.Service.CommandHandling
{
	public static class CommandHandler
	{
		private static readonly ILogger Logger = LogManager.GetLogger(nameof(TcpCommandServerJob));

		public static async Task HandleAsync(RemoteCommand command, Socket socket)
		{
			switch (command)
			{
				case HelloCommand concrete:
					Logger.Info($"Hello [{concrete.Who}]!");
					break;
				case ListCommandsCommand concrete:
					await HandleListCommands(socket);
					break;
				case ShutdownCommand concrete:
					HandleShutdown(concrete);
					break;
				case AbortShutdownCommand concrete:
					HandleAbortShutdown(concrete);
					break;
				case RestartCommand concrete:
					HandleRestart(concrete);
					break;
				case ActivateScreensaverCommand concrete:
					HandleActivateScreensaver();
					break;
				default:
					Logger.Warn($"Command [{command.CommandName}] is not handled.");
					break;
			}

			await Task.Delay(5);
			await MessageClientAsync(socket, $"Command \"{command.CommandName}\" executed.");
		}

		private static void HandleActivateScreensaver()
		{
			NativeMethods.SetScreenSaverRunning();
		}

		private static async Task HandleListCommands(Socket socket)
		{
			var responseCommand = new ListCommandsResponseCommand(new RemoteCommand[]
			{
				new HelloCommand("Server"),
				new ShutdownCommand(TimeSpan.FromSeconds(60)),
				new RestartCommand(TimeSpan.FromSeconds(60)), 
				new AbortShutdownCommand(), 
				new ActivateScreensaverCommand(), 
			});
			await ExecuteCommandAsync(responseCommand, socket);
		}

		private static async Task MessageClientAsync(Socket socket, string message)
		{
			await ExecuteCommandAsync(new DisplayMessageCommand(message), socket);
		}

		private static void HandleShutdown(ShutdownCommand concrete)
		{
			using (var process = Process.Start("shutdown", $"/s /t {concrete.Delay.Value?.TotalSeconds ?? 60}"))
			{
				process.StartInfo.CreateNoWindow = true;
				process.StartInfo.UseShellExecute = false;
				process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
			}
		}

		private static void HandleRestart(RestartCommand concrete)
		{
			using (var process = Process.Start("shutdown", $"/r /t {concrete.Delay.Value?.TotalSeconds ?? 60}"))
			{
				process.StartInfo.CreateNoWindow = true;
				process.StartInfo.UseShellExecute = false;
				process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
			}
		}

		private static void HandleAbortShutdown(AbortShutdownCommand concrete)
		{
			using (var process = Process.Start("shutdown", "/a"))
			{
				process.StartInfo.CreateNoWindow = true;
				process.StartInfo.UseShellExecute = false;
				process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
			}
		}

		private static async Task ExecuteCommandAsync(RemoteCommand remoteCommand, Socket socket)
		{
			var encryptionKey = ConfigurationManager.AppSettings["EncryptionPhrase"];
			Logger.Info($"Sending command [{remoteCommand.CommandName}] to [{socket.RemoteEndPoint}].");
			var message = remoteCommand.ToBytes(encryptionKey);
			Logger.Debug($"Length of command is [{message.Length}].");
			//			var sent = await adapter.Socket.SendAsync(new ArraySegment<byte>(message), SocketFlags.None);
//			var sent = await adapter.Socket.SendToAsync(new ArraySegment<byte>(message), SocketFlags.None, adapter.Socket.RemoteEndPoint);
			var settings = new PipeAdapterSettings();
			settings.PipeSequenceChunkifier = new PipeSequenceChunkifier(
				Encoding.UTF8.GetBytes("\n")
			);
			var adapter = new PipeAdapter(socket, settings);
			var sent = await adapter.SendAsync(message);

			Logger.Debug($"Sent [{sent}] bytes to [{socket.RemoteEndPoint}].");
		}
	}
}