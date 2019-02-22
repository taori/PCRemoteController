using Xamarin.Forms;

namespace App.Mobile.Remote.Utility
{
	public class SelectDisableBehavior : Behavior<ListView>
	{
		/// <inheritdoc />
		protected override void OnAttachedTo(ListView bindable)
		{
			base.OnAttachedTo(bindable);
			bindable.ItemSelected += BindableOnItemSelected;
		}

		private void BindableOnItemSelected(object sender, SelectedItemChangedEventArgs e)
		{
			if (sender is ListView listView)
				listView.SelectedItem = null;
		}

		/// <inheritdoc />
		protected override void OnDetachingFrom(ListView bindable)
		{
			base.OnDetachingFrom(bindable);
			bindable.ItemSelected -= BindableOnItemSelected;
		}
	}
}