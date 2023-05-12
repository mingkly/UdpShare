
using Microsoft.Maui.Storage;
using System.ComponentModel;
using System.Diagnostics;
using System.Net;
using System.Text;
using UdpQuickShare.FileActions;
using UdpQuickShare.Pages;
using UdpQuickShare.Protocols;
using UdpQuickShare.Services;

namespace UdpQuickShare;

public partial class MainPage : ContentPage,INotifyPropertyChanged
{
    public MainPage()
	{
		InitializeComponent();
    }
}

