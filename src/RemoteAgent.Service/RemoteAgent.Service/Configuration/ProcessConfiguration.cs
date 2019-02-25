using System.Collections.Generic;
using System.Configuration;

namespace RemoteAgent.Service.Configuration
{
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