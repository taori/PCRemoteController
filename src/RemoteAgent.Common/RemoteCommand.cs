using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;

namespace RemoteAgent.Common
{
	public static class RemoteCommandFactory
	{
		private static readonly Dictionary<Guid, Func<RemoteCommand>> CommandGeneratorLookup = new Dictionary<Guid, Func<RemoteCommand>>();

		static RemoteCommandFactory()
		{
			var derivates = typeof(RemoteCommand).Assembly.ExportedTypes.Where(d => typeof(RemoteCommand).IsAssignableFrom(d) && d != typeof(RemoteCommand));
			foreach (var derivate in derivates)
			{
				var generation = Activator.CreateInstance(derivate);
				if(generation is RemoteCommand command)
				CommandGeneratorLookup.Add(command.CommandId, () => Activator.CreateInstance(derivate) as RemoteCommand);
			}
		}
		public static RemoteCommand FromBytes(byte[] bytes)
		{
			var serialized = Encoding.UTF8.GetString(bytes);
			return JsonConvert.DeserializeObject<RemoteCommand>(serialized);
		}

		public static RemoteCommand FromCommand(RemoteCommand command)
		{
			if(!CommandGeneratorLookup.TryGetValue(command.CommandId, out var generator))
				throw new Exception($"Cannot generate type from {command.CommandId}.");

			var generated = generator.Invoke();
			generated.UpdateFrom(command);

			return generated;
		}
	}

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

		public byte[] ToBytes(string encryptionKey, string delimiter = "\n")
		{
			var serialized = JsonConvert.SerializeObject(this);
			return Encoding.UTF8.GetBytes(serialized + delimiter);
		}

		public void UpdateFrom(RemoteCommand command)
		{
			this.Parameters = command.Parameters;
		}
	}

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
