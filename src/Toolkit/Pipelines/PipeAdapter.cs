using System;
using System.Buffers;
using System.IO.Pipelines;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Toolkit.Pipelines
{
	/// <summary>
	/// Wraps access to System.IO.Pipelines to avoid some boilerplate
	/// </summary>
	public class PipeAdapter
	{
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
		public event EventHandler<ReadOnlySequence<byte>> Received;
		
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
		public async Task ExecuteAsync()
		{
			var pipe = new Pipe();
			var writing = FillPipeAsync(Socket, pipe.Writer);
			var reading = ReadPipeAsync(pipe.Reader);

			await Task.WhenAll(reading, writing);
		}

		private async Task FillPipeAsync(Socket socket, PipeWriter writer)
		{
			while (true)
			{
				var memory = writer.GetMemory(Settings.BufferSize);
				try
				{
					var bytesRead = await socket.ReceiveAsync(memory, Settings.FillSocketFlags);
					if (bytesRead == 0)
					{
						break;
					}

					writer.Advance(bytesRead);
				}
				catch (ObjectDisposedException odex)
				{
					break;
				}
				catch (Exception ex)
				{
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

		private async Task ReadPipeAsync(PipeReader reader)
		{
			while (true)
			{
				var result = await reader.ReadAsync();
				var buffer = result.Buffer;
				SequenceChunk? sequenceChunk = null;

				do
				{
					sequenceChunk = Settings.PipeSequenceChunkifier.Execute(buffer);

					if (sequenceChunk != null)
					{
						Received?.Invoke(this, buffer.Slice(0, sequenceChunk.Value.Position));

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

			reader.Complete();
		}
	}
}