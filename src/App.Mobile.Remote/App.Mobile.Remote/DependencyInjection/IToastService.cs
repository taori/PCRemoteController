namespace App.Mobile.Remote.DependencyInjection
{
	public enum ToastDuration
	{
		Short, Long
	}

	public interface IToastService
	{
		void DisplayToast(string message, ToastDuration duration = ToastDuration.Short);
	}
}