using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UdpQuickShare.ViewModels
{
    internal class SendingFilesViewModel
    {
        App app;
        public ObservableCollection<FileItem> Files => app.SendingFiles;
        public Command ClearCommand { get; }
        public SendingFilesViewModel() : this(App.Instance)
        {
        }
        public SendingFilesViewModel(App app)
        {
            this.app = app;
            ClearCommand = new Command(app.ClearSendingFile);
        }
    }
}
