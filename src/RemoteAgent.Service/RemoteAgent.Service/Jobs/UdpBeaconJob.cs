using System;
using System.Configuration;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using RemoteAgent.Service.Shell;
using RemoteAgent.Service.Utility;

namespace RemoteAgent.Service.Jobs
{
	public class UdpBeaconJob : JobBase
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
			try
			{
				var beaconPort = ConfigurationManager.AppSettings["BeaconPort"];
				if (!int.TryParse(beaconPort, out var parsedBeaconPort))
				{
					Logger.Error($"{beaconPort} is not a valid value for BeaconPort");
					return;
				}

				var targetEndpoint = new IPEndPoint(IPAddress.Broadcast, parsedBeaconPort);
				var datagram = Encoding.UTF8.GetBytes($"Agent [{ConfigurationManager.AppSettings["AgentName"]}] is alive.");

				using (var broadcaster = new UdpClient())
				{
					broadcaster.AllowNatTraversal(true);

					while (!cancellationToken.IsCancellationRequested)
					{
						Logger.Debug($"(>>>) [{targetEndpoint.Prettify()}] {Encoding.UTF8.GetString(datagram)}");
						await broadcaster.SendAsync(datagram, datagram.Length, targetEndpoint);
						if (!int.TryParse(ConfigurationManager.AppSettings["BeaconPollingInterval"], out var pollingInterval))
							pollingInterval = 10000;

						await Task.Delay(pollingInterval, cancellationToken);
					}
				}

				Logger.Info("Beacon terminating.");
			}
			catch (Exception e)
			{
				Logger.Error(e);
			}
		}
	}
}