using System.Net;

namespace RemoteAgent.Service.Utility
{
	public static class EndpointExtensions
	{
		public static string Prettify(this IPEndPoint source)
		{
			return $"{source.Address.ToString().PadLeft(15, ' ')}:{source.Port.ToString().PadLeft(5,' ')}";
		}
	}
}