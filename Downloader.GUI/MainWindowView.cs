using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Timers;
using Downloader;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Downloader.DownloadSites;
using Timer = System.Timers.Timer;

namespace WpfApplication1
{
    public class MainWindowView
    {
        private readonly Timer _timer = new Timer();

        public class RefreshableObservableCollection<T> : ObservableCollection<T>
        {
            public void Reset()
            {
                OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            }
        }

        public MainWindowView()
        {
            Downloads = new RefreshableObservableCollection<FileDescription>();
            _timer.Interval = 1000;
            _timer.Elapsed += _timer_Elapsed;
            _timer.Start();
        }

        public RefreshableObservableCollection<FileDescription> Downloads { get; private set; }

        private static object SyncRoot = new object();
        private void _timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (Monitor.TryEnter(SyncRoot))
            {
                try
                {
                    Update().Wait();
                }
                finally
                {
                    Monitor.Exit(SyncRoot);
                }
            }
        }

        private async Task Update()
        {
            // running
            if (Downloads.Any(f => f.Status == Downloadstatus.Downloading))
            {
                return;
            }

            FileDescription file;
            // waiting
            if ((file = Downloads.FirstOrDefault(f => f.Status == Downloadstatus.Waiting)) != null)
            {
                DateTime now = DateTime.Now;
                TimeSpan elasped = now - file.DownloadLink.WaitingSince;
                file.DownloadLink.TimeToWait -= elasped;
                file.DownloadLink.WaitingSince = now;
                file.StatusMessage = file.DownloadLink.TimeToWait.ToString(@"h\:mm\:ss");
                if (file.DownloadLink.TimeToWait > TimeSpan.Zero)
                {
                    return; // more waiting
                }
            }
            // new
            else
            {
                file = Downloads.FirstOrDefault(f => f.Status == Downloadstatus.Queued)
                    // For now, no retry
                    //?? Downloads.FirstOrDefault(f => f.Status == Downloadstatus.Error)
                    ;
                if (file == null)
                {
                    return; // nothing to do
                }
            }

            // launch download
            file.DownloadLink = await DownloadSiteFactory.Get(file.Site).GetDownloadLink(file);
            if (!string.IsNullOrEmpty(file.DownloadLink.Error))
            {
                file.StatusMessage = file.DownloadLink.Error;
                file.Status = Downloadstatus.Error;
            }
            else if (file.DownloadLink.TimeToWait > TimeSpan.Zero)
            {
                file.Status = Downloadstatus.Waiting;
            }
            else
            {
                WebHelper.DownloadFile(file);
                file.Status = Downloadstatus.Downloading;
            }
        }

        public void Up(FileDescription file)
        {
            if (Downloads.Any())
            {
                int index = Downloads.IndexOf(file);
                if (index > 0)
                {
                    Downloads.Move(index, index - 1);
                }

                Downloads.Reset();
            }
        }

        public void Down(FileDescription file)
        {
            if (Downloads.Any())
            {
                int index = Downloads.IndexOf(file);
                if (index < Downloads.Count - 1)
                {
                    Downloads.Move(index, index + 1);
                }

                Downloads.Reset();
            }
        }

        public void Add(FileDescription file)
        {
            file.SetContainer(Downloads);
            if (string.IsNullOrEmpty(file.Filename))
            {
                file.StatusMessage = "Filename not found";
                file.Status = Downloadstatus.Error;
            }
            else if (File.Exists(Path.Combine(WebHelper.DownloadPath, file.Filename)))
            {
                file.Status = Downloadstatus.Ended;
            }
            else
            {
                // starting status
                //file.Status = Downloadstatus.Queued;
                file.Status = Downloadstatus.Stopped;
            }
            
            Downloads.Add(file);
        }
    }
}
