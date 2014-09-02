using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Downloader.DownloadSites;
using HtmlAgilityPack;

namespace Downloader
{
    public static class ExploreHelper
    {
        public async static Task<FileDescription[]> ExtractDownloadDescriptions(string url)
        {
            return await Task.WhenAll((await ExtractUrls(url))
                .SelectMany(k =>
                {
                    IDownloadSite a = DownloadSiteFactory.Get(k.Key);
                    return k.Value.Select(a.ExtractDownloadDescription);
                }));
        }

        private async static Task<Dictionary<DownloadSite, List<Uri>>> ExtractUrls(string url)
        {
            Dictionary<DownloadSite, List<Uri>> uris = new Dictionary<DownloadSite, List<Uri>>();
            HtmlDocument htmlDoc = await WebHelper.GetDocument(url);

            if (htmlDoc.DocumentNode != null)
            {
                foreach (HtmlNode node in htmlDoc.DocumentNode.SelectNodes("//a"))
                {
                    string link = node.GetAttributeValue("href", null);
                    Uri linkUri;
                    if (Uri.TryCreate(link, UriKind.Absolute, out linkUri))
                    {
                        uris.TryAddDownloadPageUri(linkUri);
                    }
                }
            }

            return uris;
        }

        private static void TryAddDownloadPageUri(this Dictionary<DownloadSite, List<Uri>> uris, Uri uri)
        {
            DownloadSite site = DownloadSiteFactory.Match(uri);
            if (site != DownloadSite.None)
            {
                if (!uris.ContainsKey(site))
                {
                    uris.Add(site, new List<Uri>());
                }
                uris[site].Add(uri);
                return;
            }

            if (string.Compare(uri.Host, "adf.ly", StringComparison.InvariantCultureIgnoreCase) == 0)
            {
                Regex r = new Regex(".*(?<uri>http://.*)");
                Match m = r.Match(uri.AbsoluteUri);
                Uri subUri;
                if (m.Success && Uri.TryCreate(m.Groups["uri"].Value, UriKind.Absolute, out subUri))
                {
                    uris.TryAddDownloadPageUri(subUri);
                }
            }
        }
    }
}
