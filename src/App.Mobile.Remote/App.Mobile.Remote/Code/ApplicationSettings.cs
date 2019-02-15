using System.Threading.Tasks;

namespace App.Mobile.Remote.Code
{
	public static class ApplicationSettings
	{
		public static int UdpPort
		{
			get
			{

				if (!Xamarin.Forms.Application.Current.Properties.TryGetValue("BeaconPort", out var currentValue))
					return 8085;
				if (currentValue is int casted)
					return casted;
				return 8187;
			}
			set
			{
				Xamarin.Forms.Application.Current.Properties["BeaconPort"] = value;
			}
		}

		public static int BufferSize
		{
			get
			{

				if (!Xamarin.Forms.Application.Current.Properties.TryGetValue("BufferSize", out var currentValue))
					return 8085;
				if (currentValue is int casted)
					return casted;
				return 4096;
			}
			set
			{
				Xamarin.Forms.Application.Current.Properties["BufferSize"] = value;
			}
		}

		public static int TcpPort
		{
			get
			{
				if (!Xamarin.Forms.Application.Current.Properties.TryGetValue("TcpPort", out var currentValue))
					return 8085;
				if (currentValue is int casted)
					return casted;
				return 8085;
			}
			set
			{
				Xamarin.Forms.Application.Current.Properties["TcpPort"] = value;
			}
		}

		public static async Task SaveAsync()
		{
			await Xamarin.Forms.Application.Current.SavePropertiesAsync();
		}
	}
}