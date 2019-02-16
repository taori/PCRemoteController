using System;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Toolkit.Pipelines
{
	internal static class SocketExtensions
	{
		public static Task<int> ReceiveAsync(this Socket socket, Memory<byte> memory, SocketFlags socketFlags)
		{
			if(!MemoryMarshal.TryGetArray((ReadOnlyMemory<byte>) memory, out var segment))
				throw new InvalidOperationException("Buffer backed by array was expected.");

			return SocketTaskExtensions.ReceiveAsync(socket, segment, socketFlags);
		}
	}
}