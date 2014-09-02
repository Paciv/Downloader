using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Downloader
{
    public class DownloadLink
    {
        public Uri Link { get; set; }

        public TimeSpan TimeToWait { get; set; }

        public DateTime WaitingSince { get; set; }

        public string Error { get; set; }

        public DownloadMethod Method { get; set; }

        public List<Tuple<string, string>> PostData { get; set; }
    }

    public enum DownloadMethod
    {
        Get,
        Post,
    }
}
