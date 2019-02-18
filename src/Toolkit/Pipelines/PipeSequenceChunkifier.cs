using System;
using System.Buffers;
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

		public byte EscapeSymbol { get; }

		/// <inheritdoc />
		public PipeSequenceChunkifier(byte[] delimiter, byte escapeSymbol)
		{
			if (delimiter == null || delimiter.Length <= 0) throw new ArgumentException($"Invalid delimiter.");
			Delimiter = delimiter;
			EscapeSymbol = escapeSymbol;
		}

		public PipeSequenceChunkifier() : this(new[]{(byte)'\n'}, (byte)'\\')
		{
		}

		/// <summary>
		/// Reads the buffer and identifies seperation markers
		/// </summary>
		/// <param name="buffer"></param>
		/// <returns></returns>
		public virtual SequenceChunk? Execute(ReadOnlySequence<byte> buffer)
		{
			var sequencePosition = buffer.PositionOf(Delimiter[0]);
			if (sequencePosition == null)
				return null;

			if (Delimiter.Length > 1)
			{
				if (buffer.Length < Delimiter.Length)
					return null;

				var current = buffer.Slice(sequencePosition.Value.GetInteger(), Delimiter.Length);
				var before = buffer.Slice(sequencePosition.Value.GetInteger() - 1, 1);
				if (before.ToArray()[0] == EscapeSymbol)
					return null;

				for (int i = 0; i < current.Length; i++)
				{
					if (current.Slice(i, 1).ToArray()[0] != Delimiter[i])
						return null;
				}

//				var position = new SequencePosition(sequencePosition.Value.GetObject(), sequencePosition.Value.GetInteger());
				return new SequenceChunk(sequencePosition.Value, Delimiter.Length);
			}
			else
			{
				return new SequenceChunk(sequencePosition.Value, 1);
			}
		}
	}
}