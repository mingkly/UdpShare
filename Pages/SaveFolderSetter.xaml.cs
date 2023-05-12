using System.Collections.ObjectModel;
using UdpQuickShare.ViewModels;

namespace UdpQuickShare.Pages;

public partial class SaveFolderSetter : ContentPage
{
	public SaveFolderSetter()
	{
		InitializeComponent();
        Folders.ItemsSource = CreateFolders();
	}
    ObservableCollection<FolderItem> CreateFolders()
	{
        var collection = new ObservableCollection<FolderItem>
        {
            new FolderItem
            {
                FileType = FileActions.FileType.Text,
                Name = "文本",
                Path =App.FileSaver.GetDisplayPath(FileActions.FileType.Text),
            },
            new FolderItem
            {
                FileType = FileActions.FileType.Image,
                Name = "图片",
                Path = App.FileSaver.GetDisplayPath(FileActions.FileType.Image),
            },
            new FolderItem
            {
                FileType = FileActions.FileType.Audio,
                Name = "音频",
                Path = App.FileSaver.GetDisplayPath(FileActions.FileType.Audio),
            },
            new FolderItem
            {
                FileType = FileActions.FileType.Video,
                Name = "视频",
                Path = App.FileSaver.GetDisplayPath(FileActions.FileType.Video),
            },
            new FolderItem
            {
                FileType = FileActions.FileType.Any,
                Name = "其他",
                Path = App.FileSaver.GetDisplayPath(FileActions.FileType.Any),
            }
        };
        return collection;
    }

    private async void Folders_ItemTapped(object sender, ItemTappedEventArgs e)
    {
		var foler = e.Item as FolderItem;
		await App.FileSaver.SaveFolderToSave(foler.FileType, App.DataStore);
		foler.Path=App.FileSaver.GetDisplayPath(foler.FileType);
    }
}