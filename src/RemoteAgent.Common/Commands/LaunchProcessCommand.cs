using Newtonsoft.Json;

namespace RemoteAgent.Common.Commands
{
	public class LaunchProcessCommand : RemoteCommand
	{
		/// <inheritdoc />
		public LaunchProcessCommand() : base("C1C6AF20-5607-492C-A795-7AE3174C9827", "Launch process")
		{
			Parameters = new object[2];
		}

		/// <inheritdoc />
		public LaunchProcessCommand(string name, string path) : this()
		{
			Name = name;
			Path = path;
			CommandName = $"Launch [{name}]";
		}

		[JsonIgnore]
		public string Name
		{
			get => (string)Parameters[0];
			set => Parameters[0] = value;
		}

		[JsonIgnore]
		public string Path
		{
			get => (string)Parameters[1];
			set => Parameters[1] = value;
		}
	}
}