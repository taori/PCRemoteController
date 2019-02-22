using System;
using App.Mobile.Remote.Views;
using Xamarin.Forms;

namespace App.Mobile.Remote.Utility
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

		public static readonly BindableProperty DeactivatedBeforeProperty = BindableProperty.Create(
			propertyName: nameof(DeactivatedBefore),
			returnType: typeof(bool),
			declaringType: typeof(AppearingActivatorBehavior),
			defaultValue: null);

		public bool DeactivatedBefore
		{
			get { return (bool) GetValue(DeactivatedBeforeProperty); }
			set { SetValue(DeactivatedBeforeProperty, value); }
		}

		/// <inheritdoc />
		protected override void OnAttachedTo(Page bindable)
		{
			base.OnAttachedTo(bindable);
			bindable.Appearing += BindableOnAppearing;
			bindable.Disappearing += BindableOnDisappearing;
		}

		private async void BindableOnDisappearing(object sender, EventArgs e)
		{
			if (sender is Page page)
			{
				if (page.BindingContext is IDeactivateble activateable)
				{
					await activateable.DeactivateAsync((bool)this.GetValue(DeactivatedBeforeProperty));
					this.SetValue(DeactivatedBeforeProperty, true);
				}
			}
		}

		private async void BindableOnAppearing(object sender, EventArgs e)
		{
			if (sender is Page page)
			{
				if (page.BindingContext is INavigationAccess navigationAccess)
				{
//					var mainPage = Xamarin.Forms.Application.Current.MainPage;
					var topMostPage = GetTopMostPage(page);
					navigationAccess.NavigationProxy = topMostPage.Navigation;
				}

				if (page.BindingContext is IActivateable activateable)
				{
					await activateable.ActivateAsync((bool)this.GetValue(ActivatedBeforeProperty));
					this.SetValue(ActivatedBeforeProperty, true);
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
			bindable.Disappearing -= BindableOnDisappearing;
		}
	}
}