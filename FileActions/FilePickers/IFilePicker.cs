using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UdpQuickShare.FileActions.FilePickers
{
    public interface IFilePicker
    {
         Task<PickFileResult> PickFileAsync(FileType fileType);
        Task<IEnumerable<PickFileResult>> PickFilesAsync(FileType fileType);
        Stream OpenPickedFile(string uri);
    }
}
