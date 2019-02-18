using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using RemoteAgent.Common.Commands;

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
			var settings = new JsonSerializerSettings();
			settings.TypeNameHandling = TypeNameHandling.All;
			var serialized = Encoding.UTF8.GetString(bytes);
			var deserialized = JsonConvert.DeserializeObject<RemoteCommand>(serialized, settings);
			return deserialized;
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
}