using Newtonsoft.Json;

namespace RemoteAgent.Common.Commands
{
	public class DisplayMessageCommand : RemoteCommand
	{
		public DisplayMessageCommand()
		{
		}

		public DisplayMessageCommand(string message) : base("8B401DF3-E2C5-4DD5-BD06-3AE9A247D006", "DisplayMessage")
		{
			Parameters = new object[1];
			Message = message;
		}

		[JsonIgnore]
		public string Message
		{
			get => Parameters[0] as string;
			set => Parameters[0] = value;
		}
	}
}