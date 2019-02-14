using System.Threading.Tasks;

namespace App.Mobile.Remote.Code
{
	public static class ApplicationSettings
	{
		public static int BeaconPort
		{
			get
			{
				var currentValue = Xamarin.Forms.Application.Current.Properties["BeaconPort"];
				if (currentValue is int casted)
					return casted;

				return 8187;
			}
			set
			{
				Xamarin.Forms.Application.Current.Properties["BeaconPort"] = value;
			}
		}

		public static async Task SaveAsync()
		{
			await Xamarin.Forms.Application.Current.SavePropertiesAsync();
		}
	}
}