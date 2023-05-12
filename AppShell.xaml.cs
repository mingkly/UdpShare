using UdpQuickShare.Pages;

namespace UdpQuickShare;

public partial class AppShell : Shell
{
	public AppShell()
	{
		InitializeComponent();
		Routing.RegisterRoute("ChooseDevice", typeof(Devices));
		
	}
}
