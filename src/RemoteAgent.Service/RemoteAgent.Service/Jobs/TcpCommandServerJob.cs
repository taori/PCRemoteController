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
using RemoteAgent.Service.Shell;

namespace RemoteAgent.Service.Jobs
{
	public class TcpCommandServerJob : JobBase
	{
		private int _bufferSize;

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
				var port = ConfigurationManager.AppSettings["TcpListenerPort"] ?? "8087";
				if (!int.TryParse(port, out var parsedPort))
				{
					Logger.Error($"[{port}] is not a valid port integer.");
					return;
				}
				var bufferSize = ConfigurationManager.AppSettings["BufferSize"];
				if (!int.TryParse(bufferSize, out var parsedBufferSize))
				{
					Logger.Error($"Buffersize [{bufferSize}] is not a valid value.");
					return;
				}

				var localIp = Dns.GetHostAddresses(Dns.GetHostName()).FirstOrDefault(d => d.AddressFamily == AddressFamily.InterNetwork);
//				var listenerEndpoint = new IPEndPoint(IPAddress.Loopback, parsedPort);
				Logger.Info($"Binding TcpListener to [{localIp}].");
				var listener = new TcpListener(new IPEndPoint(localIp, parsedPort));
				Logger.Debug($"Starting listener with backlog [120]");
				listener.Start(120);
				using (listener.Server)
				{
					_bufferSize = parsedBufferSize;
					Logger.Info($"Operating with buffersize [{_bufferSize}].");

					while (!cancellationToken.IsCancellationRequested)
					{
						Logger.Debug($"Waiting for client on [{parsedPort}].");
						var remoteSocket = await listener.AcceptSocketAsync();
						Logger.Debug($"Client connected [{remoteSocket.RemoteEndPoint}].");
						ProcessLinesAsync(remoteSocket, cancellationToken);
					}
				}
			}
			catch (Exception e)
			{
				Logger.Fatal($"[{nameof(TcpCommandServerJob)}] crashed.");
				Logger.Fatal(e);
			}
		}

		private async Task ProcessLinesAsync(Socket socket, CancellationToken cancellationToken)
		{
			try
			{
				while (!cancellationToken.IsCancellationRequested)
				{
					var pipe = new Pipe();
					var writing = FillPipeAsync(socket, pipe.Writer);
					var reading = ReadPipeAsync(pipe.Reader);

					await Task.WhenAll(reading, writing);
				}
			}
			catch (OperationCanceledException) { }
			catch (Exception e)
			{
				Logger.Error(e);
			}
		}

		private async Task FillPipeAsync(Socket socket, PipeWriter writer)
		{
			while (true)
			{
				var memory = writer.GetMemory(_bufferSize);
				try
				{
					var bytesRead = await socket.ReceiveAsync(memory, SocketFlags.None);
					if (bytesRead == 0)
					{
						break;
					}
					writer.Advance(bytesRead);
				}
				catch (Exception ex)
				{
					Logger.Error(ex);
					break;
				}

				var result = await writer.FlushAsync();
				if (result.IsCompleted)
				{
					break;
				}
			}

			writer.Complete();
		}

		private async Task ReadPipeAsync(PipeReader reader)
		{
			while (true)
			{
				var result = await reader.ReadAsync();
				var buffer = result.Buffer;
				SequencePosition? position = null;

				do
				{
					position = buffer.PositionOf((byte)'\n');

					if (position != null)
					{
						await ProcessLineAsync(buffer.Slice(0, position.Value));

						buffer = buffer.Slice(buffer.GetPosition(1, position.Value));
					}
				}
				while (position != null);

				reader.AdvanceTo(buffer.Start, buffer.End);

				if (result.IsCompleted)
				{
					break;
				}
			}

			reader.Complete();
		}

		private async Task ProcessLineAsync(ReadOnlySequence<byte> slice)
		{
			var line = slice.ToString();
			Logger.Info($"Received command: {line}");
		}
	}

#if NET461
	internal static class Extensions
	{
		public static Task<int> ReceiveAsync(this Socket socket, Memory<byte> memory, SocketFlags socketFlags)
		{
			var arraySegment = GetArray(memory);
			return SocketTaskExtensions.ReceiveAsync(socket, arraySegment, socketFlags);
		}

		public static string GetString(this Encoding encoding, ReadOnlyMemory<byte> memory)
		{
			var arraySegment = GetArray(memory);
			return encoding.GetString(arraySegment.Array, arraySegment.Offset, arraySegment.Count);
		}

		private static ArraySegment<byte> GetArray(Memory<byte> memory)
		{
			return GetArray((ReadOnlyMemory<byte>)memory);
		}

		private static ArraySegment<byte> GetArray(ReadOnlyMemory<byte> memory)
		{
			if (!MemoryMarshal.TryGetArray(memory, out var result))
			{
				throw new InvalidOperationException("Buffer backed by array was expected.");
			}

			return result;
		}
	}
#endif
}