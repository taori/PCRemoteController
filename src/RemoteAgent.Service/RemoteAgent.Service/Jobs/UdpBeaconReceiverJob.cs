using System;
using System.Configuration;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using RemoteAgent.Service.Shell;

namespace RemoteAgent.Service.Jobs
{
	public class UdpBeaconReceiverJob : JobBase
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

				while (!cancellationToken.IsCancellationRequested)
				{
					using (var broadcaster = new UdpClient(new IPEndPoint(IPAddress.Any, parsedBeaconPort)))
					{
						var receive = await broadcaster.ReceiveAsync();
						Console.WriteLine($"{DateTime.Now:hh:mm:ss.} - {Encoding.UTF8.GetString(receive.Buffer)}");
					}
				}

				Logger.Info("Beacon receiver terminating.");
			}
			catch (Exception e)
			{
				Logger.Error(e);
			}
		}
	}
}