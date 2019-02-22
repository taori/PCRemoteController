using System;
using System.Threading.Tasks;

namespace App.Mobile.Remote.Utility
{
	public interface IDeactivateble
	{
		Task DeactivateAsync(bool activatedBefore);

		event EventHandler<bool> Deactivate;
	}
}