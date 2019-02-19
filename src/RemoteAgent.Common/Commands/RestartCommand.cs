using System;
using Newtonsoft.Json;

namespace RemoteAgent.Common.Commands
{
	public class RestartCommand : RemoteCommand
	{
		public RestartCommand() : base("8387B765-8071-4005-B7AF-7A860CC9F969", "Restart")
		{
			Parameters = new object[1];
		}

		[JsonIgnore]
		[ClientParameter]
		public TimeSpan Delay
		{
			get => (TimeSpan)Parameters[0];
			set => Parameters[0] = value;
		}
	}
}