using Microsoft.Maui;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Security.Isolation;
using Windows.Storage.Pickers;

namespace UdpQuickShare.FileActions.FileSavers
{
    public partial class FileSaver : IFileSaver
    {
        public readonly static string TextPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        public readonly static string ImagePath = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
        public readonly static string VideoPath = Environment.GetFolderPath(Environment.SpecialFolder.MyVideos);
        public readonly static string AudioPath = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic);
        public readonly static string OtherPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

        string textPath = TextPath;
        string imagePath = ImagePath;
        string videoPath = VideoPath;
        string audioPath = AudioPath;
        string otherPath = OtherPath;



        public FileCreateInfo CreatePlatform(string fileName, long fileLength, FileType fileType)
        {
            return CreateFile(fileName, fileLength, fileType);
        }
        public FileCreateInfo CreateFile(string fileName, long fileLength, FileType fileType)
        {
            var path = Path.Combine(GetPathPlatform(fileType), fileName);
            var fs = File.Create(path);

            fs.SetLength(fileLength);
            return new FileCreateInfo
            {
                Path = path,
                Stream = fs
            };
        }

        public Task LoadFolderToSavePlatform(IDataStore dataStore)
        {
            textPath = dataStore.Get<String>(nameof(TextPath)) ?? TextPath;
            imagePath = dataStore.Get<string>(nameof(ImagePath)) ?? ImagePath;
            videoPath = dataStore.Get<string>(nameof(VideoPath)) ?? VideoPath;
            audioPath = dataStore.Get<string>(nameof(AudioPath)) ?? AudioPath;
            otherPath = dataStore.Get<string>(nameof(OtherPath)) ?? OtherPath;
            return Task.CompletedTask;
        }

        public async Task SaveFolderToSavePlatform(FileType fileType, IDataStore dataStore)
        {
            try
            {
                var folderPicker = new FolderPicker();
                folderPicker.FileTypeFilter.Add("*");
                var hwnd = ((MauiWinUIWindow)App.Current.Windows[0].Handler.PlatformView).WindowHandle;

                // Associate the HWND with the file picker
                WinRT.Interop.InitializeWithWindow.Initialize(folderPicker, hwnd);
                var folder = await folderPicker.PickSingleFolderAsync();
                SaveFolderUri(fileType, folder.Path, dataStore);
                await LoadFolderToSave(dataStore);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"{MainThread.IsMainThread} pick folder ex {ex}");
            }

        }
        void SaveFolderUri(FileType fileType, string uri, IDataStore dataStore)
        {
            switch (fileType)
            {
                case FileType.Text:
                    dataStore.Save(nameof(TextPath), uri); break;
                case FileType.Image:
                    dataStore.Save(nameof(ImagePath), uri); break;
                case FileType.Video:
                    dataStore.Save(nameof(VideoPath), uri); break;
                case FileType.Audio:
                    dataStore.Save(nameof(AudioPath), uri); break;
                case FileType.Any:
                    dataStore.Save(nameof(OtherPath), uri); break;
            }
        }
        public string GetPathPlatform(FileType fileType)
        {
            switch (fileType)
            {
                case FileType.Text:
                    return textPath;
                case FileType.Image:
                    return imagePath;
                case FileType.Video:
                    return videoPath;
                case FileType.Audio:
                    return audioPath;
                case FileType.Any:
                default:
                    return otherPath;
            }
        }
        public Stream OpenCreatedFilePlatform(string path)
        {
            return File.OpenWrite(path);
        }
    }
}
