using System.Collections.Generic;
using System.Configuration;

namespace RemoteAgent.Service.Configuration
{
	public class ClosableProcessSettings : ConfigurationElement
	{
		[ConfigurationProperty("namePattern")]
		public string NamePattern
		{
			get => base["namePattern"] as string;
			set => base["namePattern"] = value;
		}

	}
	public class ProcessConfiguration : ConfigurationSection
	{
		[ConfigurationProperty("launcherItems")]
		public LauncherItems LauncherItems
		{
			get => base["launcherItems"] as LauncherItems;
			set => base["launcherItems"] = value;
		}

		[ConfigurationProperty("closableProcessesSettings")]
		public ClosableProcessSettings ClosableProcessesSettings
		{
			get => base["closableProcessesSettings"] as ClosableProcessSettings;
			set => base["closableProcessesSettings"] = value;
		}
	}
}