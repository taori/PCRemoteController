using System.Buffers;

namespace Toolkit.Pipelines
{
	/// <summary>
	/// Utility class to seperate stream byte sequences into individual chunks
	/// </summary>
	public class PipeSequenceChunkifier
	{
		/// <summary>
		/// Reads the buffer and identifies seperation markers
		/// </summary>
		/// <param name="buffer"></param>
		/// <returns></returns>
		public virtual SequenceChunk? Execute(ReadOnlySequence<byte> buffer)
		{
			var sequencePosition = buffer.PositionOf((byte)'\n');
			if (sequencePosition == null)
				return null;
			return new SequenceChunk(sequencePosition.Value, 1);
		}
	}
}