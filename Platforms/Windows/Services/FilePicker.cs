using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UdpQuickShare.FileActions.FilePickers
{
    public partial class FilePicker
    {
        PickOptions ToOptions(FileType fileType)
        {
            PickOptions option = null;
            if (fileType == FileType.Text)
            {
                
            }
            else if (fileType == FileType.Image)
            {
                option = new PickOptions();
                option.FileTypes = FilePickerFileType.Images;
            }
            else if (fileType == FileType.Audio)
            {

            }
            else if (fileType == FileType.Video)
            {
                option = new PickOptions();
                option.FileTypes = FilePickerFileType.Videos;
            }

            return option;
        }
        public async Task<PickFileResult> PickFilePlatformAsync(FileType fileType)
        {
            var option = ToOptions(fileType);
            var res = await Microsoft.Maui.Storage.FilePicker.Default.PickAsync(option);
            if (res == null)
            {
                return null;
            }
            using var stream=await res.OpenReadAsync();
            return new PickFileResult
            {
                Uri = res.FullPath,
                Name = res.FileName,
                Length =stream.Length,
            };
        }
        public async Task<IEnumerable<PickFileResult>> PickFilesPlatformAsync(FileType fileType)
        {
            var option = ToOptions(fileType);
            var results = await Microsoft.Maui.Storage.FilePicker.Default.PickMultipleAsync(option);
            var fileResults = new List<PickFileResult>();
            foreach (var res in results)
            {
                var stream = await res.OpenReadAsync();
                var fileRes = new PickFileResult
                {
                    Uri = res.FullPath,
                    Name = res.FileName,
                    Length = stream.Length,
                    Stream = stream,
                };
                fileResults.Add(fileRes);
            }
            return fileResults;
        }
        public Stream OpenPickedFilePlatform(string uri)
        {
            return File.OpenRead(uri);
        }
    }
}
