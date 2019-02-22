using System;
using System.Text;
using Newtonsoft.Json;

namespace RemoteAgent.Common.Commands
{
	public class RemoteCommand
	{
		private static readonly NLog.ILogger Log = NLog.LogManager.GetLogger(nameof(RemoteCommand));

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

		public byte[] ToBytes(string encryptionKey)
		{
			var settings = new JsonSerializerSettings();
			settings.TypeNameHandling = TypeNameHandling.All;
#if DEBUG
			settings.Formatting = Formatting.Indented;
#else
			settings.Formatting = Formatting.None;
#endif

			var serialized = JsonConvert.SerializeObject(this, settings);
			var combined = serialized;
			Log.Debug(combined);
			return Encoding.UTF8.GetBytes(combined);
		}

		public void UpdateFrom(RemoteCommand command)
		{
			this.Parameters = command.Parameters;
		}
	}
}