using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using App.Mobile.Remote.Code;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace App.Mobile.Remote.Views
{
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class AvailableRemoteAgents : ContentPage
	{
		public AvailableRemoteAgents()
		{
			InitializeComponent();
		}
	}
}
