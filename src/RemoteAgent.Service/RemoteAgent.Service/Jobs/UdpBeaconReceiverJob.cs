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
				var beaconPort = ConfigurationManager.AppSettings["UdpBeaconPort"];
				if (!int.TryParse(beaconPort, out var parsedBeaconPort))
				{
					Logger.Error($"{beaconPort} is not a valid value for BeaconPort");
					return;
				}

				using (var broadcaster = new UdpClient())
				{
					var ep = new IPEndPoint(IPAddress.Any, parsedBeaconPort);
					broadcaster.ExclusiveAddressUse = false;
					broadcaster.AllowNatTraversal(true);
					broadcaster.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
					broadcaster.Client.Bind(ep);
					Logger.Info($"Binding ({nameof(UdpBeaconReceiverJob)}) on [{ep}]");

					while (!cancellationToken.IsCancellationRequested)
					{
						var receive = await broadcaster.ReceiveAsync();
						Logger.Debug($"<--- [{receive.RemoteEndPoint.Prettify()}] {Encoding.UTF8.GetString(receive.Buffer)}");
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