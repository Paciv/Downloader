using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Downloader.DownloadSites
{
    public static class DownloadSiteFactory
    {
        private static readonly List<IDownloadSite> Sites = new List<IDownloadSite>
        {
            new UpToBox(),
            new Onefichier(),
        };

        public static DownloadSite Match(Uri uri)
        {
            IDownloadSite site = Sites.FirstOrDefault(downloadSite => downloadSite.Match(uri));
            return site == null ? DownloadSite.None : site.Site;
        }

        public static IDownloadSite Get(DownloadSite site)
        {
            return Sites.FirstOrDefault(s => s.Site == site);
        }
    }

    public interface IDownloadSite
    {
        bool Match(Uri uri);
        Task<FileDescription> ExtractDownloadDescription(Uri uri);
        Task<DownloadLink> GetDownloadLink(FileDescription file);
        DownloadSite Site { get; }
    }

    public enum DownloadSite
    {
        None,
        UpToBox,
        Onefichier,
    }
}
