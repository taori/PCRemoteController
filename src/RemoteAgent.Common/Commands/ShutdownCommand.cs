using System;
using Newtonsoft.Json;

namespace RemoteAgent.Common.Commands
{
	public class ShutdownCommand : RemoteCommand
	{
		public ShutdownCommand() : base("B07E6779-9EF5-4069-BC45-4D13A17190CA", "Shutdown")
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