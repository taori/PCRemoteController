using System;
using System.Buffers;
using System.IO.Pipelines;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Toolkit.Pipelines
{
	/// <summary>
	/// Wraps access to System.IO.Pipelines to avoid some boilerplate
	/// </summary>
	public class PipeAdapter
	{
		private static readonly NLog.ILogger Log = NLog.LogManager.GetLogger(nameof(PipeAdapter));

		/// <summary>
		/// Socket which is used for pipeline operations
		/// </summary>
		public Socket Socket { get; }

		/// <summary>
		/// Settings used to interact with the pipleline including hooks to manipulate the behavior
		/// </summary>
		public PipeAdapterSettings Settings { get; }

		/// <summary>
		/// This event should be used to process the results received by the pipleline
		/// </summary>
		public event EventHandler<byte[]> Received;
		
		/// <inheritdoc />
		public PipeAdapter(Socket socket) : this(socket, new PipeAdapterSettings())
		{
		}

		public PipeAdapter(Socket socket, PipeAdapterSettings settings)
		{
			Socket = socket;
			Settings = settings;
		}
		
		/// <summary>
		/// Executes read/write operations using the given socket
		/// </summary>
		public async Task ListenAsync()
		{
			var pipe = new Pipe();
			var writing = FillPipeAsync(Socket, pipe.Writer);
			var reading = ReadPipeAsync(pipe.Reader);

			await Task.WhenAll(reading, writing);
		}

		private async Task FillPipeAsync(Socket socket, PipeWriter writer)
		{
			while (socket.Connected)
			{
				var memory = writer.GetMemory(Settings.BufferSize);
				try
				{
					var bytesRead = await socket.ReceiveAsync(memory, Settings.FillSocketFlags);
					if (bytesRead == 0)
					{
						Log.Debug($"0 bytes received.");
						break;
					}

					Log.Debug($"Received [{bytesRead}] bytes on [{socket.RemoteEndPoint}] -> [{socket.LocalEndPoint}].");

					writer.Advance(bytesRead);
				}
				catch (ObjectDisposedException odex)
				{
					Log.Error($"Object disposed exception occured.");
					Log.Error(odex);
					break;
				}
				catch (SocketException sex) when (ExpectedException(sex))
				{
					Log.Error($"SocketException [{sex.SocketErrorCode.ToString()}] occured.");
					Log.Error(sex);
					break;
				}
				catch (Exception ex)
				{
					Log.Error(ex);
					if (Settings.ThrowOnException)
						throw;

					Settings?.ExceptionHandler?.Invoke(ex);
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

		private static readonly SocketError[] ExpectedSocketErrors = {
			SocketError.Disconnecting,
			SocketError.ConnectionReset,
			SocketError.ConnectionAborted
		};

		private bool ExpectedException(SocketException sex)
		{
			Settings?.ExceptionHandler?.Invoke(sex);

			return ExpectedSocketErrors.Contains(sex.SocketErrorCode);
		}

		private async Task ReadPipeAsync(PipeReader reader)
		{
			while (true)
			{
				var result = await reader.ReadAsync();
				var buffer = result.Buffer;
				SequenceChunk? sequenceChunk = null;

				do
				{
					Log.Trace($"Reading from pipe.");

					if (result.Buffer.Length > 0)
						Log.Debug($"Reading from pipe using bufferLength [{result.Buffer.Length}].");

					sequenceChunk = Settings.PipeSequenceChunkifier.Execute(buffer);

					if (sequenceChunk != null)
					{
						Log.Debug($"Sequence found. Broadcasting [{sequenceChunk.Value.Position}] bytes to event subscribers.");
						Received?.Invoke(this, DecodeTransmission(buffer, sequenceChunk));

						buffer = buffer.Slice(buffer.GetPosition(sequenceChunk.Value.Length, sequenceChunk.Value.Position));
					}
				}
				while (sequenceChunk?.Position != null);

				reader.AdvanceTo(buffer.Start, buffer.End);

				if (result.IsCompleted)
				{
					break;
				}
			}

			Log.Trace($"Reader completed.");
			reader.Complete();
		}

		private static byte[] DecodeTransmission(ReadOnlySequence<byte> buffer, SequenceChunk? sequenceChunk)
		{
			if (!sequenceChunk.HasValue)
				return Array.Empty<byte>();

			var slice = buffer.Slice(0, sequenceChunk.Value.Position);
			var sliceArray = slice.ToArray();
			var decoded = Convert.FromBase64String(Encoding.UTF8.GetString(sliceArray));
			Log.Debug($"Decoded transmission with length [{slice.Length}]: {decoded}");
			return decoded;
		}

		public async Task<int> SendAsync(byte[] message)
		{
			var converted = string.Format("{0}{1}", Convert.ToBase64String(message), Encoding.UTF8.GetString(Settings.PipeSequenceChunkifier.Delimiter));
			var bytes = Encoding.UTF8.GetBytes(converted);
			Log.Debug($"Sending [{bytes.Length}] bytes as escaped sequence ({converted}) through {nameof(PipeAdapter)}.");
			return await Socket.SendToAsync(new ArraySegment<byte>(bytes), SocketFlags.None, Socket.RemoteEndPoint);
		}
	}
}