using System.Threading.Tasks;

namespace App.Mobile.Remote.Code
{
	public interface IActivateable
	{
		Task ActivateAsync(bool activatedBefore);
	}
}