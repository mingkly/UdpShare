using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UdpQuickShare.FileActions.FileSavers
{
    public interface IFileSaver
    {
        FileCreateInfo Create(string fileName,long fileLength, FileType fileType);
        Task SaveFolderToSave(FileType fileType, IDataStore dataStore);
        Task LoadFolderToSave(IDataStore dataStore);
        string GetPath(FileType fileType);
        Task SaveAsync(FileCreateInfo fileCreateInfo);
        Stream OpenCreatedFile(string path);
        string GetDisplayPath(FileType fileType);
    }
}
