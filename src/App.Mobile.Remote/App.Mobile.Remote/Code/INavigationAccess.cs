using Xamarin.Forms;

namespace App.Mobile.Remote.Code
{
	public interface INavigationAccess
	{
		INavigation NavigationProxy { get; set; }
	}
}