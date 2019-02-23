using Newtonsoft.Json;

namespace RemoteAgent.Common.Commands
{
	public class KillProcessCommand : RemoteCommand
	{
		/// <inheritdoc />
		public KillProcessCommand() : base("D17E3AED-4E22-429F-8722-788DA732EF04", "Kill process")
		{
			Parameters = new object[2];
		}

		public KillProcessCommand(string name, int processId) : this()
		{
			Name = name;
			ProcessId = processId;
			CommandName = $"Kill [{name} ({processId})]";
		}

		[JsonIgnore]
		public string Name
		{
			get => (string)Parameters[0];
			set => Parameters[0] = value;
		}

		[JsonIgnore]
		public long ProcessId
		{
			get => (long)Parameters[1];
			set => Parameters[1] = value;
		}
	}
}