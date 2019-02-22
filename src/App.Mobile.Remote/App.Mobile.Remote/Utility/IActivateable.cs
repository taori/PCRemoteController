using System.Threading.Tasks;

namespace App.Mobile.Remote.Utility
{
	public interface IActivateable
	{
		Task ActivateAsync(bool activatedBefore);
	}
}