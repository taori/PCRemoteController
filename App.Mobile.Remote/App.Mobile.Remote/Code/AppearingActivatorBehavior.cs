using System;
using Xamarin.Forms;

namespace App.Mobile.Remote.Code
{
	public class AppearingActivatorBehavior : Behavior<Page>
	{
		public static readonly BindableProperty ActivatedBeforeProperty = BindableProperty.Create(
			propertyName: nameof(ActivatedBefore),
			returnType: typeof(bool),
			declaringType: typeof(AppearingActivatorBehavior),
			defaultValue: null);

		public bool ActivatedBefore
		{
			get { return (bool) GetValue(ActivatedBeforeProperty); }
			set { SetValue(ActivatedBeforeProperty, value); }
		}

		/// <inheritdoc />
		protected override void OnAttachedTo(Page bindable)
		{
			base.OnAttachedTo(bindable);
			bindable.Appearing += BindableOnAppearing;
		}

		private async void BindableOnAppearing(object sender, EventArgs e)
		{
			if (sender is Page page)
			{
				if (page.BindingContext is IActivateable activateable)
				{
					await activateable.ActivateAsync((bool)this.GetValue(ActivatedBeforeProperty));
					this.SetValue(ActivatedBeforeProperty, true);
				}
				if (page.BindingContext is INavigationAccess navigationAccess)
				{
					var topMostPage = GetTopMostPage(page);
					navigationAccess.NavigationProxy = topMostPage.Navigation;
				}
			}
		}

		private Page GetTopMostPage(Page page)
		{
			var current = page;
			while (current != null)
			{
				if (current.Parent is Page parentPage)
				{
					current = parentPage;
				}
				else
				{
					break;
				}

				if (current is NavigationPage navPage)
					return navPage;
			}

			return current;
		}

		/// <inheritdoc />
		protected override void OnDetachingFrom(Page bindable)
		{
			base.OnDetachingFrom(bindable);
			bindable.Appearing -= BindableOnAppearing;
		}
	}
}