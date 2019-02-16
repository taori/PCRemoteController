using System;

namespace Toolkit.Pipelines
{
	/// <summary>
	/// Separator struct, used to identify the position and length of a sequence
	/// </summary>
	public struct SequenceChunk
	{
		public SequenceChunk(SequencePosition position, int length)
		{
			Position = position;
			Length = length;
		}

		/// <summary>
		/// Position used to find out how long a sequence is
		/// </summary>
		public SequencePosition Position { get; set; }

		/// <summary>
		/// Length of sequence separator
		/// </summary>
		public int Length { get; set; }
	}
}