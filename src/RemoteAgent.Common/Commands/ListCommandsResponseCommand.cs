using Newtonsoft.Json;

namespace RemoteAgent.Common.Commands
{
	public class ListCommandsResponseCommand : RemoteCommand
	{
		public ListCommandsResponseCommand() : base("4F624A05-EB0F-42E0-A9B1-FC7F401B7BDD", "ListCommandsResponse")
		{
			Parameters = new object[1];
		}

		public ListCommandsResponseCommand(RemoteCommand[] commands) : this()
		{
			Commands = commands;
		}

		[JsonIgnore]
		public RemoteCommand[] Commands
		{
			get => Parameters[0] as RemoteCommand[];
			set => Parameters[0] = value;
		}
	}
}