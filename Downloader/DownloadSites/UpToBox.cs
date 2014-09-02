using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using HtmlAgilityPack;

namespace Downloader.DownloadSites
{
    public class UpToBox : IDownloadSite
    {
        public bool Match(Uri uri)
        {
            return string.Compare(uri.Host, "uptobox.com", StringComparison.InvariantCultureIgnoreCase) == 0;
        }

        public DownloadSite Site
        {
            get { return DownloadSite.UpToBox; }
        }

        public async Task<FileDescription> ExtractDownloadDescription(Uri uri)
        {
            FileDescription d = new FileDescription
            {
                Uri = uri,
                Site = Site,
            };
            HtmlDocument doc = await WebHelper.GetDocument(uri);
            if (doc.DocumentNode != null)
            {
                HtmlNode fileNameNode = doc.DocumentNode.SelectSingleNode("//input[@name='fname']");
                HtmlNode titleNode = doc.DocumentNode.SelectSingleNode("//div[@class='para_title']");

                if (fileNameNode != null)
                {
                    d.Filename = fileNameNode.GetAttributeValue("value", null);
                }

                if (titleNode != null)
                {
                    if (string.IsNullOrWhiteSpace(d.Filename))
                    {
                        d.Filename = titleNode.InnerText.Substring(0, titleNode.InnerText.LastIndexOf('(') - 1);
                    }
                    d.FileSize = titleNode.InnerText.EndSubString('(', ')');
                }
            }

            return d;
        }

        public async Task<DownloadLink> GetDownloadLink(FileDescription file)
        {
            DownloadLink downloadLink = new DownloadLink
            {
                TimeToWait = TimeSpan.MaxValue,
                Method = DownloadMethod.Get,
            };

            HtmlDocument doc = await WebHelper.GetDocument(file.Uri);
            await GetDownloadLink(downloadLink, file, doc);

            return downloadLink;
        }

        private static async Task GetDownloadLink(DownloadLink downloadLink, FileDescription file, HtmlDocument doc)
        {
            if (doc.DocumentNode == null)
            {
                downloadLink.Error = "Unable to fetch or parse Uri";
                return;
            }

            HtmlNode formNode = doc.DocumentNode.SelectSingleNode("//form[@name='F1']");
            if (formNode == null)
            {
                downloadLink.Error = "Unable to find form on page";
                return;
            }

            HtmlNode submitNode = formNode.SelectSingleNode("//input[@type='submit' and @id='btn_download']");
            if (submitNode != null)
            {
                HtmlDocument downloadLinkPage = await WebHelper.PostDocument(file.Uri,
                    formNode.SelectNodes("//input[@type='hidden']")
                        .Select(n => new Tuple<string, string>(
                            n.GetAttributeValue("name", null), n.GetAttributeValue("value", null))));
                if (downloadLinkPage.DocumentNode == null)
                {
                    downloadLink.Error = "Unable to load or parse download link page";
                    return;
                }
                foreach (HtmlNode node in downloadLinkPage.DocumentNode.SelectNodes("//a"))
                {
                    Uri downloadLinkUri;
                    string downloadLinkUrl = node.GetAttributeValue("href", null);
                    if (Uri.TryCreate(downloadLinkUrl, UriKind.Absolute, out downloadLinkUri) &&
                        downloadLinkUrl.EndsWith(file.Filename))
                    {
                        downloadLink.Link = downloadLinkUri;
                        downloadLink.TimeToWait = TimeSpan.Zero;
                    }
                }
                if (downloadLink.Link == null)
                {
                    downloadLink.Error = "Unable to find or parse download link on page";
                }
            }
            else
            {
                Regex r = new Regex("(?<minutes>\\d+) minutes, (?<seconds>\\d+) seconds");
                Match match = r.Match(doc.DocumentNode.InnerHtml);
                if (!match.Success)
                {
                    downloadLink.Error = "Unable to find wait time on page";
                    return;
                }
                int minutes;
                int seconds;
                if (match.Groups["minutes"] == null || !int.TryParse(match.Groups["minutes"].Value, out minutes) ||
                    match.Groups["seconds"] == null || !int.TryParse(match.Groups["seconds"].Value, out seconds))
                {
                    downloadLink.Error = "Unable to parse waiting time on page";
                    return;
                }
                downloadLink.TimeToWait = new TimeSpan(0, minutes, seconds);
                downloadLink.WaitingSince = DateTime.Now;
            }
        }
    }
}
