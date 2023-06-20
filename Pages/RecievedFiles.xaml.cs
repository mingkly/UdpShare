

using Microsoft.Maui.Controls.PlatformConfiguration;
using Microsoft.Maui.Handlers;
using System.Diagnostics;

namespace UdpQuickShare.Pages;

public partial class RecievedFiles : ContentPage
{
	
	public RecievedFiles()
	{
		InitializeComponent();
        Content = new Views.FilesVerticalView();
    }
    bool lastIsVertical = true;
    protected override void OnSizeAllocated(double width, double height)
    {
        base.OnSizeAllocated(width, height);
        bool isVertical = width < height;
        if (lastIsVertical != isVertical)
        {
            if (width > height)
            {
                Content = new Views.FilesHorizontalView();
            }
            else
            {
                Content = new Views.FilesVerticalView();
            }
            lastIsVertical = isVertical;
        }
    }

}