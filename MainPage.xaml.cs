
using System.ComponentModel;


namespace UdpQuickShare;

public partial class MainPage : ContentPage,INotifyPropertyChanged
{
    public MainPage()
	{
		InitializeComponent();
        Content = new Views.MainPageVerticalView();
    }
    bool lastIsVertical=true;
    protected override void OnSizeAllocated(double width, double height)
    {
        base.OnSizeAllocated(width, height);
        bool isVertical = width < height;
        if(lastIsVertical!= isVertical)
        {
            if (width > height)
            {
                Content = new Views.MainPageHorizontalView();
            }
            else
            {
                Content = new Views.MainPageVerticalView();
            }
            lastIsVertical = isVertical;
        }     
    }

}

