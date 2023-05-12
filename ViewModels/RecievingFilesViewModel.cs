using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UdpQuickShare.ViewModels
{
    internal class RecievingFilesViewModel:ViewModelBase
    {
        App app;
        public ObservableCollection<FileItem> Files => app.Files;
        public Command ClearCommand { get; }
        public bool AutoOpen
        {
            get=>app.AutoOpen;
            set
            {
                if(value!=app.AutoOpen)
                {
                    app.AutoOpen = value;
                    OnPropertyChanged();
                }
            }
        }
        public RecievingFilesViewModel():this(App.Instance)
        {
        }
        public RecievingFilesViewModel(App app)
        {
            this.app = app;
            ClearCommand = new Command(app.ClearRecievingFile);
        }
    }
}
