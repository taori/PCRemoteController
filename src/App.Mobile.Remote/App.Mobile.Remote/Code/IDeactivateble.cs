using System.Threading.Tasks;

namespace App.Mobile.Remote.Code
{
	public interface IDeactivateble
	{
		Task DeactivateAsync(bool activatedBefore);
	}
}