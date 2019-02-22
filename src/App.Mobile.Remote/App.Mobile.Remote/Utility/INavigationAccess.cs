using Xamarin.Forms;

namespace App.Mobile.Remote.Utility
{
	public interface INavigationAccess
	{
		INavigation NavigationProxy { get; set; }
	}
}