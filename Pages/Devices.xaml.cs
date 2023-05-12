using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
using UdpQuickShare.ViewModels;

namespace UdpQuickShare.Pages;

public partial class Devices : ContentPage
{
	public Devices()
	{
		InitializeComponent();
	}
    private async void Button_Clicked(object sender, EventArgs e)
    {
		await Navigation.PopModalAsync();
    }
}