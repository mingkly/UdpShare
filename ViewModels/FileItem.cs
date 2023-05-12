using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace UdpQuickShare.ViewModels
{
    public class FileItem:ViewModelBase
    {
        App app;
        public ICommand PauseOrStartCommand { get; }
        public ICommand OpenCommand { get; }
        public ICommand CopyCommand { get; }
        public ICommand InfoCommand { get; }
        public ICommand DeleteCommand { get; }
        public FileItem(App app,bool isSendingFile)
        {
            this.app = app;
            IsSendingFile= isSendingFile;
            PauseOrStartCommand = new Command(() =>
            {
                if (isSendingFile)
                {
                    app.StopOrResumeSendingFile(this);
                }
                else
                {
                    app.StopOrResumeRecievingFile(this);
                }
            });
            DeleteCommand = new Command(() =>
            {
                if (isSendingFile)
                {
                    app.RemoveSendingFile(FileId);
                }
                else
                {
                    app.RemoveRecievingFile(FileId);
                }
            });
            OpenCommand=new AsyncRelayCommand(() =>app.OpenRecievedFile(this));
            CopyCommand = new AsyncRelayCommand(() => app.CopyText(this));
            InfoCommand = new AsyncRelayCommand(() => app.InfoReciveFile(this));
        }
        public bool IsSendingFile { get; set; }
        public uint FileId { get; set; }
        bool working;
        public bool Working
        {
            get { return working; }
            set=>Set(ref working, value);
        }
        public DateTime CreateTime { get; set; }
        string name;
        public string Name
        {
            get => name;
            set=>Set(ref name, value);
        }
        string path;
        public string Path
        {
            get => path;
            set => Set(ref path, value);
        }
        string description;
        public string Description
        {
            get => description;
            set => Set(ref description, value);
        }
        double percent;
        public double Percent
        {
            get => percent; set => Set(ref percent, value);
        }
        long time;
        public long Time
        {
            get=> time;
            set => Set(ref time, value);
        }
        long speed;
        public long Speed
        {
            get => speed;
            set => Set(ref speed, value);
        }
        long length;
        public long Length
        {
            get => length;
            set => Set(ref length, value);
        }
        FileItemState state;
        public FileItemState State
        {
            get => state;
            set => Set(ref state, value);
        }

        string currentSizeString;
        public string CurrentSizeString
        {
            get => currentSizeString;
            set=> Set(ref currentSizeString, value);
        }
        string totalSizeString;
        public string TotalSizeString
        {
            get => totalSizeString;
            set => Set(ref totalSizeString, value);
        }
        string leftTimeString;
        public string LeftTimeString
        {
            get => leftTimeString;
            set=> Set(ref leftTimeString, value);
        }
        string speedString;
        public string SpeedString
        {
            get => speedString;
            set => Set(ref speedString, value);
        }
        string percentString;
        public string PercentString
        {
            get => percentString; set => Set(ref percentString, value);
        }
        protected override void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);
            ThisOnPropertyChanged(e.PropertyName);
        }
        void ThisOnPropertyChanged(string propertyName)
        {
            if (propertyName == nameof(Percent))
            {
                CurrentSizeString = ToSizeString((long)(Length * Percent));
                PercentString = $"{Percent*100:0}%";
                long leftTime= (long)(Length *(1- Percent)/Speed);
                if (Percent >= 1)
                {
                    LeftTimeString = "";
                }
                else if(Speed<=0)
                {
                    LeftTimeString ="---";
                }
                else
                {
                    LeftTimeString = ToTimeString(leftTime);
                }
                
            }
            else if (propertyName == nameof(Speed))
            {
                long leftTime = (long)(Length * (1 - Percent) / Speed);
                if (Percent >= 1)
                {
                    LeftTimeString = "";
                }
                else if(speed<=0)
                {
                    LeftTimeString ="---";
                }
                else
                {
                    LeftTimeString = ToTimeString(leftTime);
                }
                SpeedString = ToSpeedString(Speed);
            }
            else if (propertyName == nameof(Length))
            {
                TotalSizeString = ToSizeString(Length);
            }
        }


        public static string ToSizeString(long size)
        {
            int kb = 1024;
            int mb = 1024 * 1024;
            int gb= 1024 * 1024*1024;
            if (size < kb)
            {
                return $"{size}B";
            }
            else if (size < mb)
            {
                return $"{size /kb}{(double)(size % kb) / kb:.00}KB";
            }
            else if (size < gb)
            {
                return $"{size / mb}{(double)(size % (mb)) / mb:.00}MB";
            }
            else
            {
                return $"{size / gb}{(double)(size % (gb)) / gb:.00}GB";
            }
        }
        public static string ToSpeedString(long speed)
        {
            return $"{ToSizeString(speed)}/S";
        }
        public static string ToTimeString(long time)
        {
            int m = 60;
            int h = 60 * 60;
            int d = h * 24;
            if(time < m)
            {
                return $"{time}秒";
            }
            else if(time< h)
            {
                return $"{time / m}分钟{(time % m)}秒";
            }
            else if (time < d)
            {
                return $"{time / h}小时{(time % h)/m}分钟";
            }
            else
            {
                return $"{time / d}天{(time % d)/h}小时";
            }
        }
    }
}
