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
}