using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using RemoteAgent.Service.Shell;

namespace RemoteAgent.Service.Jobs
{
	public class TcpCommandClientJob : JobBase
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
			await Task.Delay(5000);

			var localEndpoint = new IPEndPoint(Dns.GetHostAddresses(Dns.GetHostName()).FirstOrDefault(d => d.AddressFamily == AddressFamily.InterNetwork), 8085);
			Logger.Debug($"Creating TcpClient with local address [{localEndpoint}].");
			using (var client = new TcpClient())
			{
				Logger.Debug($"Connecting local client to [{localEndpoint}].");
				await client.ConnectAsync(localEndpoint.Address, 8085);
				using (var stream = client.GetStream())
				{
					while (true)
					{
						Logger.Debug($"Sending abc and waiting 10 seconds.");
						await stream.WriteAsync(Encoding.UTF8.GetBytes("abc"), 0, 3);
						await Task.Delay(10000);
					}
				}
			}
		}
	}
}