using Android.Content;
using Android.Content.PM;
using Android.Net;
using Android.OS;
using Android.Provider;
using AndroidX.DocumentFile.Provider;
using Java.IO;
using Java.Net;
using Kotlin.IO;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UdpQuickShare.Platforms.Android.Services;
using Debug = System.Diagnostics.Debug;
using Uri = Android.Net.Uri;

namespace UdpQuickShare.FileActions.FileSavers
{
    public partial class FileSaver : IFileSaver
    {
        public readonly static string TextPath = "Download/UdpShare/Texts";
        public readonly static string ImagePath = "Download/UdpShare/Images";
        public readonly static string VideoPath = "Download/UdpShare/Videos";
        public readonly static string AudioPath = "Download/UdpShare/Audios";
        public readonly static string OtherPath = "Download/UdpShare/Others";

        string textPath=TextPath;
        string imagePath=ImagePath;
        string videoPath=VideoPath;
        string audioPath=AudioPath;
        string otherPath=OtherPath;

        MainActivity Context=>MainActivity.Instance;

        public FileCreateInfo CreatePlatform(string fileName, long fileLength, FileType fileType)
        {
            var path = GetPathPlatform(fileType);
            FileCreateInfo fileResult;
            if (path.StartsWith("content"))
            {
                fileResult=CreateFileInDocument(fileName, fileLength,Uri.Parse(path));
            }
            else
            {
                fileResult = CreateFileInDownload(fileName, fileLength, path);
            }
            return fileResult;
        }
        public FileCreateInfo CreateFileInDownload(string fileName,long fileLength,string relativePath)
        {
            try
            {
                Stream stream;
                string path;
                if (Build.VERSION.SdkInt > BuildVersionCodes.Q)
                {
                    Uri downloadUri;
                    downloadUri = MediaStore.Files.GetContentUri(MediaStore.VolumeExternal);
                    ContentValues file = new ContentValues();
                    file.Put("_display_name", $"{fileName}");
                    file.Put("relative_path", $"{relativePath}");
                    file.Put("_size", fileLength);
                    var fileUri = Context.ContentResolver.Insert(downloadUri, file);
                    var fd = Context.ContentResolver.OpenFileDescriptor(fileUri, "rw",null);
                    stream = new JavaStreamWrapper(fd);
                    path =fileUri.ToString();

                }
                else
                {
                    var status = Permissions.CheckStatusAsync<Permissions.StorageRead>().Result;
                    if (status != PermissionStatus.Granted)
                    {
                        Permissions.RequestAsync<Permissions.StorageRead>().Wait();
                        Permissions.RequestAsync<Permissions.StorageWrite>().Wait();
                    }
                    var folder = Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryDownloads);
                    var basePath = relativePath.Replace("Download/", "");
                    var folders = basePath.Split('/');
                    Java.IO.File file=folder;
                    file = new Java.IO.File(file, $"{basePath}/");

                    if (!file.Exists())
                    {
                        if (!file.Mkdirs())
                        {
                            throw new Exception();
                        }                       
                    }
                    file = new Java.IO.File(file, $"{fileName}");
                    stream =new JavaStreamWrapper(file);
                    path = file.AbsolutePath;
                }
                return new FileCreateInfo()
                {
                    Path =path,
                    Stream = stream,
                };
            }
            catch(Exception e)
            {
                Debug.WriteLine(e);
            }
            return null;
        }
        public FileCreateInfo CreateFileInDocument(string fileName,long fileLength,Uri folderUri)
        {
            try
            {
                DocumentFile file;
                if (DocumentsContract.IsDocumentUri(Context, folderUri))
                {
                    file = DocumentFile.FromSingleUri(Context, folderUri);
                }
                else
                {
                    file = DocumentFile.FromTreeUri(Context, folderUri);
                }
                file = file.CreateFile("*/*", fileName);
                
                ContentValues fileValue = new ContentValues();
              
                var fd = Context.ContentResolver.OpenFileDescriptor(file.Uri, "rw", null);
                var stream = new JavaStreamWrapper(fd);
                return new FileCreateInfo()
                {
                    Path = file.ToString(),
                    Stream = stream,
                };
            }
            catch(Exception ex) { Debug.WriteLine(ex); }
            return null;

        }
        public Task LoadFolderToSavePlatform(IDataStore dataStore)
        {
            textPath = dataStore.Get<String>(nameof(TextPath)) ?? TextPath;
            imagePath = dataStore.Get<string>(nameof(ImagePath)) ?? ImagePath;
            videoPath=dataStore.Get<string>(nameof(VideoPath))?? VideoPath;
            audioPath = dataStore.Get<string>(nameof(AudioPath)) ?? AudioPath;
            otherPath = dataStore.Get<string>(nameof(OtherPath)) ?? OtherPath;
            return Task.CompletedTask;
        }

        public async Task SaveFolderToSavePlatform(FileType fileType, IDataStore dataStore)
        {
            Context.PickFolderTaskCompletionSource = new TaskCompletionSource<FilePickers.PickFileResult>();
            var intent = new Intent(Intent.ActionOpenDocumentTree);
            Context.StartActivityForResult(intent, Context.PickFolderId);
            var res = await Context.PickFolderTaskCompletionSource.Task;
            SaveFolderUri(fileType, res.Uri, dataStore);
            await LoadFolderToSave(dataStore);
        }
        void SaveFolderUri(FileType fileType,string uri,IDataStore dataStore)
        {
            switch(fileType)
            {
                case FileType.Text:
                    dataStore.Save(nameof(TextPath), uri);
                    break;
                case FileType.Image:
                    dataStore.Save(nameof(ImagePath), uri);
                    break;
                case FileType.Video:
                    dataStore.Save(nameof(VideoPath),uri); break;
                case FileType.Audio:
                    dataStore.Save(nameof(AudioPath), uri);break;
                case FileType.Any:
                    dataStore.Save(nameof(OtherPath),uri);break;
            }
        }
        public string GetPathPlatform(FileType fileType)
        {
            switch(fileType)
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
            if (path.StartsWith("content"))
            {
                var fd = Context.ContentResolver.OpenFileDescriptor(Uri.Parse(path), "rw", null);
                return new JavaStreamWrapper(fd);
            }
            else
            {
                return new JavaStreamWrapper(new Java.IO.File(path));
            } 
            
        }
    }
}
