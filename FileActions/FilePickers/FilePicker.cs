using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace UdpQuickShare.FileActions.FilePickers
{
    public partial class FilePicker : IFilePicker
    {
        public FilePicker() { }



        public Stream OpenPickedFile(string uri)
        {
            try
            {
                return OpenPickedFilePlatform(uri);
            }
            catch { }
            return null;
            
        }

        public Task<PickFileResult> PickFileAsync(FileType fileType)
        {
            try
            {
                return PickFilePlatformAsync(fileType);
            }
            catch(Exception ex)
            {
                Debug.WriteLine($"pick file error,{ex}");
            }
            return Task.FromResult<PickFileResult>(null);
        }

        public Task<IEnumerable<PickFileResult>> PickFilesAsync(FileType fileType)
        {
            try
            {
                return PickFilesPlatformAsync(fileType);
            }
            catch { }
            return Task.FromResult<IEnumerable<PickFileResult>>(null);
           
        }
    }
}
