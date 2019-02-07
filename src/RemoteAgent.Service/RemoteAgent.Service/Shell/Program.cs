using System;
using System.Collections;
using System.Configuration.Install;
using System.Reflection;
using System.ServiceProcess;
using System.Threading.Tasks;
using NLog;

namespace RemoteAgent.Service.Shell
{
	class Program
	{
		private static readonly ILogger Logger = LogManager.GetLogger(nameof(ServiceCore));

		static async Task Main(string[] args)
		{
			if (args.Length == 0)
			{
				Logger.Info("Program executing without arguments.");
			}
			else
			{
				Logger.Info($"Program executing with arguments: {string.Join(" ", args)}");
			}

			if (Environment.UserInteractive)
			{
				await ProcessInteractiveAsync(args);
			}
			else
			{
				await LaunchAsServiceAsync(args);
			}
		}

		private static async Task ProcessInteractiveAsync(string[] args)
		{
			try
			{
				var executionMode = GetExecutionMode(args);
				Logger.Trace($"Executing Interactive with executionMode: {executionMode}");

				switch (executionMode)
				{
					case "-console":
						await RunAsConsoleAsync(args);
						break;

					case "-install":
						RunInstall();
						break;

					case "-uninstall":
						RunUninstall();
						break;

					default:
						Console.WriteLine($"Unknown argument: {executionMode}");
						break;
				}
			}
			catch (Exception e)
			{
				Logger.Fatal(e);
				Console.ForegroundColor = ConsoleColor.Red;
				Console.WriteLine(e.ToString());
				Console.ReadKey();
			}
		}

		private static string GetExecutionMode(string[] args)
		{
			if (args.Length == 0)
				return "-console";
			return args[0].ToLower();
		}

		private static void RunUninstall()
		{
			using (var ti = GetTransactedInstaller())
			{
				ti.Uninstall(null);
			}
		}

		private static void RunInstall()
		{
			using (var ti = GetTransactedInstaller())
			{
				ti.Install(new Hashtable());
			}
		}

		private static TransactedInstaller GetTransactedInstaller()
		{
			var ti = new TransactedInstaller();
			ti.Installers.Add(new DefaultServiceInstaller());
			ti.Context = new InstallContext("", new string[] { $"/assemblypath={Assembly.GetExecutingAssembly().Location}" });
			return ti;
		}

		private static Task LaunchAsServiceAsync(string[] args)
		{
			var serviceCore = new ServiceCore();
			ServiceBase.Run(serviceCore);
			return Task.CompletedTask;
		}

		private static async Task RunAsConsoleAsync(string[] args)
		{
			var core = new ServiceCore();
			await core.ExecuteAsync(args);
		}
	}
}
