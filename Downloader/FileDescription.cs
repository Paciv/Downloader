using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Downloader.DownloadSites;

namespace Downloader
{
    public class FileDescription : INotifyPropertyChanged
    {
        private Collection<FileDescription> _containerCollection;
        public void SetContainer(Collection<FileDescription> containerCollection)
        {
            _containerCollection = containerCollection;
        }

        public bool Selected { get; set; }

        public int O
        {
            get { return _containerCollection.IndexOf(this) + 1; }
        }

        private string _fileName;
        public string Filename { get { return _fileName; } set { _fileName = value; NotifyPropertyChanged(); } }

        public string FileSize { get; set; }

        private Downloadstatus _status;
        public Downloadstatus Status
        {
            get { return _status; }
            set { _status = value; NotifyPropertyChanged(); }
        }

        private string _statusMessage;
        public string StatusMessage { get { return _statusMessage; } set { _statusMessage = value; NotifyPropertyChanged(); } }

        public Uri Uri { get; set; }

        public DownloadSite Site { get; set; }

        public DownloadLink DownloadLink { get; set; }

        public void Start()
        {
            switch (Status)
            {
                case Downloadstatus.Stopped:
                case Downloadstatus.Error:
                    Status = Downloadstatus.Queued;
                    StatusMessage = null;
                    break;
            }
        }

        public void Stop()
        {
            switch (Status)
            {
                case Downloadstatus.Queued:
                case Downloadstatus.Waiting:
                    Status = Downloadstatus.Stopped;
                    break;
                case Downloadstatus.Downloading:
                    Status = Downloadstatus.Stopped;
                    // TODO Stop the downloading thread
                    break;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged([CallerMemberName]string propertyName = null)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }

    public enum Downloadstatus
    {
        Stopped,
        Waiting,
        Queued,
        Downloading,
        Ended,
        Error,
    }
}
