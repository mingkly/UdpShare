using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UdpQuickShare.FileActions.FileSavers
{
    public partial class FileSaver : IFileSaver
    {
        public FileSaver() { }
        public FileCreateInfo Create(string fileName, long fileLength, FileType fileType)
        {
            return CreatePlatform(fileName, fileLength, fileType);
        }

        public Task LoadFolderToSave(IDataStore dataStore)
        {
            return LoadFolderToSavePlatform(dataStore);
        }

        public Task SaveFolderToSave(FileType fileType, IDataStore dataStore)
        {
            return SaveFolderToSavePlatform(fileType, dataStore);
        }

        public string GetPath(FileType fileType)
        {
            return GetPathPlatform(fileType);
        }
        public Task SaveAsync(FileCreateInfo fileCreateInfo)
        {
            using (fileCreateInfo.Stream)
            return Task.CompletedTask;
        }
        public Stream OpenCreatedFile(string path)
        {
            return OpenCreatedFilePlatform(path);
        }
        public string GetDisplayPath(FileType fileType)
        {
            var uri=GetPath(fileType);
#if ANDROID
            return Android.Net.Uri.Decode(uri).Split(':').Last();
#else
            return uri;
#endif
        }
    }
}
