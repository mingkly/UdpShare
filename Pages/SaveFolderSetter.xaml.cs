using System.Collections.ObjectModel;
using UdpQuickShare.ViewModels;

namespace UdpQuickShare.Pages;

public partial class SaveFolderSetter : ContentPage
{
	public SaveFolderSetter()
	{
		InitializeComponent();
    BindableLayout.SetItemsSource(Folders, CreateFolders());
#if WINDOWS
UseDirect.IsVisible = false;
#endif
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        UseDirectPathSwitch.IsToggled = FileManager.CanUseDirectPath;
    }
    ObservableCollection<FolderItem> CreateFolders()
	{
        var collection = new ObservableCollection<FolderItem>
        {
            new FolderItem
            {
                FileType = FileActions.FileType.Text,
                Name = "ÎÄ±¾",
                Path =FileSaveManager.GetSaveFolder(FileActions.FileType.Text),
            },
            new FolderItem
            {
                FileType = FileActions.FileType.Image,
                Name = "Í¼Æ¬",
                Path = FileSaveManager.GetSaveFolder(FileActions.FileType.Image),
            },
            new FolderItem
            {
                FileType = FileActions.FileType.Audio,
                Name = "ÒôÆµ",
                Path = FileSaveManager.GetSaveFolder(FileActions.FileType.Audio),
            },
            new FolderItem
            {
                FileType = FileActions.FileType.Video,
                Name = "ÊÓÆµ",
                Path = FileSaveManager.GetSaveFolder(FileActions.FileType.Video),
            },
            new FolderItem
            {
                FileType = FileActions.FileType.Any,
                Name = "ÆäËû",
                Path = FileSaveManager.GetSaveFolder(FileActions.FileType.Any),
            }
        };
        return collection;
    }

    private async void Folders_ItemTapped(object sender, ItemTappedEventArgs e)
    {
		var foler = e.Item as FolderItem;
		await FileSaveManager.ChooseSaveFolder(foler.FileType);
		foler.Path=FileSaveManager.GetSaveFolder(foler.FileType); 
    }

    private async void Switch_Toggled(object sender, ToggledEventArgs e)
    {
        FileManager.UseDirectPath = e.Value;
        App.DataStore.Save("UseDirectPath", e.Value);
        if (e.Value)
        {
            var success = await FileManager.RequsetUseDirectPathNeed();
            if (!success)
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    UseDirectPathSwitch.IsToggled = false;
                });
            }
        }
    }

    private async void TapGestureRecognizer_Tapped(object sender, TappedEventArgs e)
    {
        var foler =(sender as BindableObject).BindingContext as FolderItem;
        await FileSaveManager.ChooseSaveFolder(foler.FileType);
        foler.Path = FileSaveManager.GetSaveFolder(foler.FileType);
    }
}