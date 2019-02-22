using System;
using Newtonsoft.Json;
using RemoteAgent.Common.Serialization;

namespace RemoteAgent.Common.Commands
{
	public class ShutdownCommand : RemoteCommand
	{
		public ShutdownCommand() : base("B07E6779-9EF5-4069-BC45-4D13A17190CA", "Shutdown")
		{
			Parameters = new object[1];
		}

		public ShutdownCommand(TimeSpan? delay) : this()
		{
			Delay = new JsonSerializedType<TimeSpan?>(delay);
		}

		[JsonIgnore]
		[ClientParameter]
		[JsonConverter(typeof(UnknownObjectConverter))]
		public JsonSerializedType<TimeSpan?> Delay
		{
			get => (JsonSerializedType<TimeSpan?>)Parameters[0];
			set => Parameters[0] = value;
		}
	}
}