using Newtonsoft.Json;

namespace RemoteAgent.Common.Commands
{
	public class HelloCommand : RemoteCommand
	{
		public HelloCommand() : base("86C91EAA-8C8E-4BE4-A9A0-1DA1A67D3C47", "Hello")
		{
			Parameters = new object[1];
		}

		public HelloCommand(string who) : this()
		{
			Who = who;
		}

		[JsonIgnore]
		public string Who
		{
			get => Parameters[0] as string;
			set => Parameters[0] = value;
		}
	}
}