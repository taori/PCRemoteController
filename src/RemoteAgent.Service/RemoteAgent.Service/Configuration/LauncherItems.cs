using System.Configuration;

namespace RemoteAgent.Service.Configuration
{
	public class LauncherItems : ConfigurationElementCollection
	{
		/// <inheritdoc />
		protected override ConfigurationElement CreateNewElement()
		{
			return new LauncherItem();
		}

		/// <inheritdoc />
		protected override object GetElementKey(ConfigurationElement element)
		{
			return ((LauncherItem)element).Name;
		}
	}
}