using System;
using System.Net.Sockets;

namespace Toolkit.Pipelines
{
	/// <summary>
	/// Settings and extension point for the given PipeAdapter
	/// </summary>
	public class PipeAdapterSettings
	{
		private PipeSequenceChunkifier _pipeSequenceChunkifier = new PipeSequenceChunkifier();

		/// <summary>
		/// Utility class used to seperate sequences
		/// </summary>
		public PipeSequenceChunkifier PipeSequenceChunkifier
		{
			get => _pipeSequenceChunkifier;
			set => _pipeSequenceChunkifier = value ?? throw new ArgumentNullException(nameof(PipeSequenceChunkifier));
		}

		/// <summary>
		/// Handler for exception cases
		/// </summary>
		public Action<Exception> ExceptionHandler { get; set; }

		/// <summary>
		/// Socket flags used during write operations
		/// </summary>
		public SocketFlags FillSocketFlags { get; set; } = SocketFlags.None;

		/// <summary>
		/// Whether or not exceptions should be thrown
		/// </summary>
		public bool ThrowOnException { get; set; } = false;

		/// <summary>
		/// Buffer size used for write operations
		/// </summary>
		public int BufferSize { get; set; } = 2048;
	}
}