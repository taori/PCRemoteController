using System.Configuration;

namespace RemoteAgent.Service.Configuration
{
	public class LauncherItem : ConfigurationElement
	{
		[ConfigurationProperty("name")]
		public string Name
		{
			get => base["name"] as string;
			set => base["name"] = value;
		}

		[ConfigurationProperty("path")]
		public string Path
		{
			get => base["path"] as string;
			set => base["path"] = value;
		}

		[ConfigurationProperty("runas")]
		public bool Runas
		{
			get => (bool)base["runas"];
			set => base["runas"] = value;
		}
	}
}