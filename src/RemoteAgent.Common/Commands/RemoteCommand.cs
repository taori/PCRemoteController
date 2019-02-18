using System;
using System.Text;
using Newtonsoft.Json;

namespace RemoteAgent.Common.Commands
{
	public class RemoteCommand
	{
		/// <inheritdoc />
		public RemoteCommand(string commandId, string commandName)
		{
			CommandId = Guid.Parse(commandId);
			CommandName = commandName;
		}

		/// <inheritdoc />
		public RemoteCommand()
		{
		}

		public Guid CommandId { get; set; }

		public string CommandName { get; set; }

		public object[] Parameters { get; set; }

		public byte[] ToBytes(string encryptionKey, string delimiter)
		{
			var settings = new JsonSerializerSettings();
			settings.TypeNameHandling = TypeNameHandling.All;
			var serialized = JsonConvert.SerializeObject(this, settings);
			var combined = serialized + delimiter;
			return Encoding.UTF8.GetBytes(combined);
		}

		public void UpdateFrom(RemoteCommand command)
		{
			this.Parameters = command.Parameters;
		}
	}
}