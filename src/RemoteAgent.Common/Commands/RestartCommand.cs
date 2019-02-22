using System;
using Newtonsoft.Json;
using RemoteAgent.Common.Serialization;

namespace RemoteAgent.Common.Commands
{
	public class RestartCommand : RemoteCommand
	{
		public RestartCommand() : base("8387B765-8071-4005-B7AF-7A860CC9F969", "Restart")
		{
			Parameters = new object[1];
		}

		public RestartCommand(TimeSpan? delay) : this()
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