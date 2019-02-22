using System;
using System.Buffers;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace Toolkit.Pipelines
{
	/// <summary>
	/// Utility class to seperate stream byte sequences into individual chunks
	/// </summary>
	public class PipeSequenceChunkifier
	{
		public byte[] Delimiter { get; }

		/// <inheritdoc />
		public PipeSequenceChunkifier(byte[] delimiter)
		{
			if (delimiter == null || delimiter.Length <= 0) throw new ArgumentException($"Invalid delimiter.");
			Delimiter = delimiter;
		}

		public PipeSequenceChunkifier() : this(new[] { (byte)'\n' })
		{
		}

		/// <summary>
		/// Reads the buffer and identifies seperation markers
		/// </summary>
		/// <param name="buffer"></param>
		/// <returns></returns>
		public virtual SequenceChunk? Execute(ReadOnlySequence<byte> buffer)
		{
			var originalPosition = buffer.PositionOf(Delimiter[0]);
			if (originalPosition == null)
				return null;

			var whiteSpan = new ReadOnlySpan<byte>(Delimiter);

			var sequencePosition = FindDelimittedPosition(buffer, originalPosition.Value.GetObject(), whiteSpan);
			if (sequencePosition == null)
				return null;

			if (Delimiter.Length > 1)
				return new SequenceChunk(sequencePosition.Value, Delimiter.Length);

			return new SequenceChunk(sequencePosition.Value, 1);
		}

		private SequencePosition? FindDelimittedPosition(in ReadOnlySequence<byte> buffer, object bufferSegment, ReadOnlySpan<byte> whiteSpan)
		{
			if (buffer.IsSingleSegment)
			{
				var position = GetPositionInSpan(buffer.First.Span, whiteSpan);
				if (position >= 0)
					return new SequencePosition(bufferSegment, position);
			}
			else
			{
				var start = buffer.Start;
				while (buffer.TryGet(ref start, out var memory, true))
				{
					if(GetPositionInSpan(memory.Span, whiteSpan) is var position && position >= 0)
						return new SequencePosition(bufferSegment, position);
				}
			}

			return null;
		}

		private int GetPositionInSpan(in ReadOnlySpan<byte> firstSpan, in ReadOnlySpan<byte> whiteSpan)
		{
			var whiteMatch = firstSpan.IndexOf(whiteSpan);
			return whiteMatch;
		}
	}
}