using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HtmlAgilityPack;

namespace Downloader.DownloadSites
{
    public class Onefichier : IDownloadSite
    {
        public bool Match(Uri uri)
        {
            return uri.Host.EndsWith("1fichier.com", StringComparison.InvariantCultureIgnoreCase);
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
                HtmlNode formNode = doc.DocumentNode.SelectSingleNode("//form");
                if (formNode == null)
                {
                    d.Status = Downloadstatus.Error;
                    d.StatusMessage = "Unable to find form tag on page";
                    return d;
                }

                HtmlNode tableNode = formNode.SelectSingleNode("//table");
                if (tableNode == null)
                {
                    d.Status = Downloadstatus.Error;
                    d.StatusMessage = "Unable to find table tag on page";
                    return d;
                }

                HtmlNodeCollection rows = tableNode.SelectNodes("//tr");
                if (rows == null)
                {
                    d.Status = Downloadstatus.Error;
                    d.StatusMessage = "Unable to find table description rows on page";
                    return d;
                }

                foreach (HtmlNode row in rows)
                {
                    HtmlNode keyNode = row.SelectSingleNode("th");
                    HtmlNode valueNode = row.SelectSingleNode("td");
                    if (keyNode != null && valueNode != null)
                    {
                        if (keyNode.InnerText.Contains("Nom du fichier"))
                        {
                            d.Filename = valueNode.InnerText;
                        }
                        if (keyNode.InnerText.Contains("Taille"))
                        {
                            d.FileSize = valueNode.InnerText;
                        }
                    }
                }
            }

            return d;
        }

        public async Task<DownloadLink> GetDownloadLink(FileDescription file)
        {
            DownloadLink downloadLink = new DownloadLink
            {
                TimeToWait = TimeSpan.MaxValue,
                Method = DownloadMethod.Post,
                Link = file.Uri,
            };

            HtmlDocument doc = await WebHelper.GetDocument(file.Uri);
            HtmlNode divNode = doc.DocumentNode.SelectSingleNode("//input[@type='submit']").ParentNode.ParentNode.ParentNode;
            if (divNode == null)
            {
                file.Status = Downloadstatus.Error;
                file.StatusMessage = "Unable to find div tag on page";
                return downloadLink;
            }

            bool tableFound = false;
            foreach (HtmlNode childNode in divNode.ChildNodes)
            {
                if (childNode.Name.Equals("table", StringComparison.InvariantCultureIgnoreCase))
                {
                    tableFound = true;
                }
                if (childNode.Name.Equals("div", StringComparison.InvariantCultureIgnoreCase))
                {
                    if (!tableFound)
                    {
                        // found div before table => warning
                        downloadLink.TimeToWait = new TimeSpan(0, 0, 1, 0);
                        downloadLink.WaitingSince = DateTime.Now;
                        return downloadLink;
                    }
                    else
                    {
                        // found div after table look for submit
                        downloadLink.TimeToWait = TimeSpan.Zero;
                        return downloadLink;
                    }
                }
            }

            file.Status = Downloadstatus.Error;
            file.StatusMessage = "Unable to either submit or warning";
            return downloadLink;
        }

        public DownloadSite Site
        {
            get { return DownloadSite.Onefichier; }
        }
    }
}
