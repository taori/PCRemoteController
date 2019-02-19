using System;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
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

		public static async Task HandleAsync(RemoteCommand command, PipeAdapter adapter)
		{
			switch (command)
			{
				case HelloCommand concrete:
					Logger.Info($"Hello [{concrete.Who}]!");
					break;
				case ListCommandsCommand concrete:
					await HandleListCommands(adapter);
					break;
				case ShutdownCommand concrete:
					HandleShutdown();
					break;
				case AbortShutdownCommand concrete:
					HandleAbortShutdown();
					break;
				case RestartCommand concrete:
					HandleRestart();
					break;
				case ActivateScreensaverCommand concrete:
					HandleActivateScreensaver();
					break;
				default:
					Logger.Warn($"Command [{command.CommandName}] is not handled.");
					break;
			}

			await Task.Delay(5);
//			await MessageClientAsync(adapter, $"Command \"{command.CommandName}\" executed.");
		}

		private static void HandleActivateScreensaver()
		{
			NativeMethods.SetScreenSaverRunning();
		}

		private static async Task HandleListCommands(PipeAdapter adapter)
		{
			var responseCommand = new ListCommandsResponseCommand(new RemoteCommand[]
			{
				new HelloCommand("Server"),
				new ShutdownCommand(),
				new RestartCommand(), 
				new AbortShutdownCommand(), 
				new ActivateScreensaverCommand(), 
			});
			await ExecuteCommandAsync(responseCommand, adapter);
		}

		private static async Task MessageClientAsync(PipeAdapter adapter, string message)
		{
			await ExecuteCommandAsync(new DisplayMessageCommand(message), adapter);
		}

		private static void HandleShutdown()
		{
			using (var process = Process.Start("shutdown", "/s /t 60"))
			{
				process.StartInfo.CreateNoWindow = true;
				process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
			}
		}

		private static void HandleRestart()
		{
			using (var process = Process.Start("shutdown", "/r /t 60"))
			{
				process.StartInfo.CreateNoWindow = true;
				process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
			}
		}

		private static void HandleAbortShutdown()
		{
			using (var process = Process.Start("shutdown", "/a"))
			{
				process.StartInfo.CreateNoWindow = true;
				process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
			}
		}

		private static async Task ExecuteCommandAsync(RemoteCommand remoteCommand, PipeAdapter adapter)
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