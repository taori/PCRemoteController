using System.Text;
using System.Threading.Tasks;

namespace App.Mobile.Remote.Code
{
	public static class ApplicationSettings
	{
		public static byte[] EncryptionKey => Encoding.UTF8.GetBytes(EncryptionPhrase);
		public static byte[] EncryptionSalt => Encoding.UTF8.GetBytes(EncryptionInitializerVector);

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

		public static string EncryptionInitializerVector
		{
			get
			{
				if (!Xamarin.Forms.Application.Current.Properties.TryGetValue("EncryptionInitializerVector", out var currentValue))
					return "EncryptionInitializerVector";
				if (currentValue is string casted)
					return casted;
				return "EncryptionInitializerVector";
			}
			set
			{
				Xamarin.Forms.Application.Current.Properties["EncryptionInitializerVector"] = value;
			}
		}

		public static string EncryptionPhrase
		{
			get
			{
				if (!Xamarin.Forms.Application.Current.Properties.TryGetValue("EncryptionPhrase", out var currentValue))
					return "EncryptionPhrase";
				if (currentValue is string casted)
					return casted;
				return "EncryptionPhrase";
			}
			set
			{
				Xamarin.Forms.Application.Current.Properties["EncryptionPhrase"] = value;
			}
		}

		public static string CommandDelimiter
		{
			get
			{
				if (!Xamarin.Forms.Application.Current.Properties.TryGetValue("CommandDelimiter", out var currentValue))
					return "\n";
				if (currentValue is string casted)
					return casted;
				return "\n";
			}
			set
			{
				Xamarin.Forms.Application.Current.Properties["CommandDelimiter"] = value;
			}
		}

		public static async Task SaveAsync()
		{
			await Xamarin.Forms.Application.Current.SavePropertiesAsync();
		}
	}
}