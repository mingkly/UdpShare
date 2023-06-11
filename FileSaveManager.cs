using MauiFileSaver;
using MKFilePicker;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UdpQuickShare.FileActions;
using UdpQuickShare.FileActions.FileSavers;

namespace UdpQuickShare
{
    internal class FileSaveManager
    {
        public class FileResultJson
        {
            public string FileName { get; set; }

            public string FullPath { get; set; }

            public string PlatformPath { get; set; }
            public static FileResultJson GetFromFilePickFesult(FilePickResult filePickResult)
            {
                return new FileResultJson
                {
                    FileName = filePickResult.FileName,
                    FullPath = filePickResult.FullPath,
                    PlatformPath = filePickResult.PlatformPath,
                };
            }
        }

        public const string Videos = "UdpShare/Videos";
        public const string Images = "UdpShare/Images";
        public const string Texts = "UdpShare/Texts";
        public const string Audios = "UdpShare/Audios";
        public const string Others = "UdpShare/Others";
        public static IDataStore DataStore { get; set; }
        public static DefaultFolderProvider DefaultFolderProvider= new DefaultFolderProvider();
        public static int MapFileType(FileType fileType)
        {
            var type=fileType switch
            {
                FileType.Text => SaveFolderType.Text,
                FileType.Audio => SaveFolderType.Audio,
                FileType.Video => SaveFolderType.Video,
                FileType.Image => SaveFolderType.Image,
                _=>SaveFolderType.Other,
            };
            return (int)type;
        }
        public static async Task ChooseSaveFolder(FileType fileType)
        {
            try
            {
                var res = await MKFilePicker.MKFilePicker.PickFolderAsync(null);
                if (res != null)
                {
                    DataStore.Save(fileType.ToString(), FileResultJson.GetFromFilePickFesult(res));
                }
            }
            catch(Exception e)
            {
                App.Log(e);
            }

        }
        static string GetDefaultChildFolder(FileType fileType)
        {
            return fileType switch
            {
                FileType.Text => Texts,
                FileType.Image => Images,
                FileType.Audio => Audios,
                FileType.Video => Videos,
                _ => Others,
            };
        }
        public static string GetSaveFolder(FileType fileType)
        {
            var dataStoreRes = DataStore.Get<FileResultJson>(fileType.ToString());
            if(dataStoreRes != null)
            {
                return dataStoreRes.FullPath;
            }
            else
            {
                return Path.Combine(DefaultFolderProvider.GetDefaultFolder(4),GetDefaultChildFolder(fileType));
            }
        }

        public static FileCreateInfo CreateFile(string fileName,FileType fileType)
        {
            
            var dataStoreRes = DataStore.Get<FileResultJson>(fileType.ToString());
            if (dataStoreRes != null)
            {
                var file = MKFilePicker.MKFilePicker.CreateFile(dataStoreRes.PlatformPath, fileName);
                return new FileCreateInfo()
                {
                    Stream = FileManager.OpenReadWriteFile(file.PlatformPath,file.FullPath),
                    Path = file.FullPath,
                    PlatformPath = file.PlatformPath,
                    FileType = fileType
                };
            }
            else
            {
                try
                {
                    var file = MauiFileSaver.MKFileSaver.Save((int)SaveFolderType.Other, Path.Combine(GetDefaultChildFolder(fileType), fileName));
                    return new FileCreateInfo
                    {
                        FileType = fileType,
                        Path = file.FullPath,
                        PlatformPath=file.PlatformPath,
                        Stream =FileManager.OpenWriteFile(file.PlatformPath,file.FullPath),
                    };
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex);
                }
                return null;

            }
        }
    }
}
