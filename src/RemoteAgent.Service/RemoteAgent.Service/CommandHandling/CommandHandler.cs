using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32;
using NLog;
using RemoteAgent.Common.Commands;
using RemoteAgent.Service.Configuration;
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
				case ToggleSoundMuteCommand concrete:
					HandleToggleSoundMute(concrete);
					break;
				case AbortShutdownCommand concrete:
					HandleAbortShutdown(concrete);
					break;
				case RestartCommand concrete:
					HandleRestart(concrete);
					break;
				case ScreenOffCommand concrete:
					HandleScreenOff();
					break;
				case ScreenOnCommand concrete:
					HandleScreenOn();
					break;
				case LaunchProcessCommand concrete:
					HandleLaunchProcess(concrete);
					await Task.Delay(300);
					await HandleListCommands(socket);
					break;
				case KillProcessCommand concrete:
					HandleKillProcess(concrete);
					await Task.Delay(50);
					await HandleListCommands(socket);
					break;
				default:
					Logger.Warn($"Command [{command.CommandName}] is not handled.");
					break;
			}
		}

		private static void HandleKillProcess(KillProcessCommand concrete)
		{
			Logger.Info($"Killing process [{concrete.ProcessId}].");
			var process = Process.GetProcesses().FirstOrDefault(d => d.Id == concrete.ProcessId);
			if (process == null)
			{
				Logger.Warn($"Process id [{concrete.ProcessId}] not found.");
				return;
			}

			try
			{
				process.Kill();
			}
			catch (Exception e)
			{
				Logger.Error(e);
			}
		}

		private static void HandleLaunchProcess(LaunchProcessCommand concrete)
		{
			var availableProcesses = GetLauncherCommands();
			var match = availableProcesses.Cast<LaunchProcessCommand>().FirstOrDefault(d => d.Path == concrete.Path);
			if (match == null)
			{
				Logger.Warn($"Attempted execution of {concrete.Path} ass launch command - not found.");
				return;
			}

			var configuration = GetProcessConfiguration();
			var configurationMatch = configuration.LauncherItems.Cast<LauncherItem>().FirstOrDefault(d => d.Name == match.Name);
			if (configurationMatch == null)
			{
				Logger.Error($"Unable to locate matching configuration entry for name: [{match.Name}]");
				return;
			}

			var startInfo = new ProcessStartInfo("cmd", $"/C start {configurationMatch.Path}");
			startInfo.UseShellExecute = true;
			startInfo.CreateNoWindow = true;
			startInfo.WindowStyle = ProcessWindowStyle.Hidden;
			if (configurationMatch.Runas)
				startInfo.Verb = "runas";

			using (var process = Process.Start(startInfo))
			{
				process.WaitForExit();
			}
		}

		private static void HandleScreenOn()
		{
			NativeMethods.Monitor.On();
		}

		private static void HandleScreenOff()
		{
			NativeMethods.Monitor.Off();
		}

		private static void HandleToggleSoundMute(ToggleSoundMuteCommand concrete)
		{
			NativeMethods.ToggleMute();
		}

		private static async Task HandleListCommands(Socket socket)
		{
			var responseCommand = new ListCommandsResponseCommand(new RemoteCommand[]
			{
				new HelloCommand("Server"),
				new ToggleSoundMuteCommand(), 
				new ShutdownCommand(TimeSpan.FromSeconds(60)),
				new RestartCommand(TimeSpan.FromSeconds(60)), 
				new AbortShutdownCommand(), 
				new ScreenOffCommand(), 
				new ScreenOnCommand(), 
			}
				.Concat(GetLauncherCommands()
				.Concat(GetClosableProcessCommands())
			).ToArray());
			await ExecuteCommandAsync(responseCommand, socket);
		}

		private static IEnumerable<RemoteCommand> GetClosableProcessCommands()
		{
			var launcherConfiguration = GetProcessConfiguration();
			var processes = Process.GetProcesses();
			foreach (var process in DoesMatchProcessFilter(launcherConfiguration, processes))
			{
				yield return new KillProcessCommand(process.ProcessName, process.Id);
			}
		}

		private static IEnumerable<Process> DoesMatchProcessFilter(ProcessConfiguration processConfiguration, Process[] processes)
		{
			var namePattern = processConfiguration.ClosableProcessesSettings.NamePattern;
			if (namePattern == "*")
			{
				foreach (var process in processes)
				{
					yield return process;
				}
			}
			else
			{
				var patterns = namePattern.Split(new char[] {','}, StringSplitOptions.RemoveEmptyEntries);
				foreach (var process in processes)
				{
					foreach (var pattern in patterns)
					{
						if (process.ProcessName.Contains(pattern))
						{
							yield return process;
							break;
						}
					}
				}
			}
		}

		private static IEnumerable<RemoteCommand> GetLauncherCommands()
		{
			var launcherConfiguration = GetProcessConfiguration();
			foreach (LauncherItem item in launcherConfiguration.LauncherItems)
			{
//				if (File.Exists(item.Path))
				{
					yield return new LaunchProcessCommand(item.Name, item.Path);
				}
			}
		}

		private static ProcessConfiguration GetProcessConfiguration()
		{
			return ConfigurationManager.GetSection("processConfiguration") as ProcessConfiguration;
		}

		private static void HandleShutdown(ShutdownCommand concrete)
		{
			using (var process = Process.Start("shutdown", $"/s /t {concrete.Delay.Value?.TotalSeconds ?? 60}"))
			{
				process.StartInfo.CreateNoWindow = true;
				process.StartInfo.UseShellExecute = true;
				process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
			}
		}

		private static void HandleRestart(RestartCommand concrete)
		{
			using (var process = Process.Start("shutdown", $"/r /t {concrete.Delay.Value?.TotalSeconds ?? 60}"))
			{
				process.StartInfo.CreateNoWindow = true;
				process.StartInfo.UseShellExecute = true;
				process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
			}
		}

		private static void HandleAbortShutdown(AbortShutdownCommand concrete)
		{
			using (var process = Process.Start("shutdown", "/a"))
			{
				process.StartInfo.CreateNoWindow = true;
				process.StartInfo.UseShellExecute = true;
				process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
			}
		}

		public static async Task ExecuteCommandAsync(RemoteCommand remoteCommand, Socket socket)
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