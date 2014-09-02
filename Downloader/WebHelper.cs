using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using HtmlAgilityPack;

namespace Downloader
{
    public static class WebHelper
    {
        public const string DownloadPath = @"C:\Users\pbarry\Downloads";

        public static string CreatePath(string fileName)
        {
            return Path.Combine(DownloadPath, fileName);
        }

        public static string CreateTempPath(string fileName)
        {
            return Path.Combine(DownloadPath, fileName + ".tmp");
        }

        public static void DownloadFile(FileDescription file)
        {
            switch (file.DownloadLink.Method)
            {
                case DownloadMethod.Get:
                    DownloadFileGet(file);
                    break;
                case DownloadMethod.Post:
                    DownloadFilePost(file);
                    break;
                default:
                    file.Status = Downloadstatus.Error;
                    file.StatusMessage = "Unload download method";
                    break;
            }
        }

        public static void DownloadFileGet(FileDescription file)
        {
            WebClient client = new WebClient();
            client.DownloadFileCompleted += client_DownloadFileCompleted(file);
            client.DownloadProgressChanged += client_DownloadProgressChanged(file);
            client.DownloadFileAsync(file.DownloadLink.Link, Path.Combine(DownloadPath, file.Filename));
        }

        public async static void DownloadFilePost(FileDescription file)
        {
            Exception error = null;
            try
            {
                using (HttpWebResponse response = await Post(file.DownloadLink.Link, file.DownloadLink.PostData))
                {
                    using (Stream reponseStream = response.GetResponseStream())
                    {
                        using (FileStream fileStream = File.Create(CreateTempPath(file.Filename)))
                        {
                            long totalRecieved = 0;
                            byte[] buffer = new byte[1024];
                            int resultLength;
                            while ((resultLength = reponseStream.Read(buffer, 0, buffer.Length)) != 0)
                            {
                                totalRecieved += resultLength;
                                fileStream.Write(buffer, 0, resultLength);
                                manual_DownloadProgressChanged(file, resultLength, totalRecieved);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                error = ex;
            }
            client_DownloadFileCompleted(file)(null, new AsyncCompletedEventArgs(error, false, null));
        }

        static AsyncCompletedEventHandler client_DownloadFileCompleted(FileDescription file)
        {
            return (sender, e) =>
            {
                if (e.Error != null)
                {
                    file.Status = Downloadstatus.Error;
                    file.StatusMessage += " - Error during download : " + e.Error.Message;
                    string path = CreateTempPath(file.Filename);
                    if (File.Exists(path))
                    {
                        File.Delete(path);
                    }
                }
                else
                {
                    file.Status = Downloadstatus.Ended;
                    File.Move(CreateTempPath(file.Filename), CreatePath(file.Filename));
                }
            };
        }

        static DownloadProgressChangedEventHandler client_DownloadProgressChanged(FileDescription file)
        {
            return (sender, e) =>
            {
                file.StatusMessage = (e.ProgressPercentage / 100D).ToString("p");
            };
        }

        static void manual_DownloadProgressChanged(FileDescription file, long bytesRecieved, long totalBytesRecieved)
        {
            file.StatusMessage = totalBytesRecieved.GetHumanReadableFileSize();
        }

        internal async static Task<HtmlDocument> GetDocument(string url)
        {
            Uri uri;
            HtmlDocument doc = new HtmlDocument();
            if (Uri.TryCreate(url, UriKind.Absolute, out uri))
            {
                return await GetDocument(uri);
            }
            return doc;
        }

        internal async static Task<HtmlDocument> GetDocument(Uri uri)
        {
            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(await DownloadPage(uri));
            return doc;
        }

        internal async static Task<string> DownloadPage(Uri uri)
        {
            HttpWebRequest request = WebRequest.CreateHttp(uri);
            using (HttpWebResponse response = (HttpWebResponse)await request.GetResponseAsync())
            {
                using (Stream s = response.GetResponseStream())
                {
                    using (TextReader tr = new StreamReader(s))
                    {
                        return await tr.ReadToEndAsync();
                    }
                }
            }
        }

        internal async static Task<HtmlDocument> PostDocument(Uri uri, IEnumerable<Tuple<string, string>> postData)
        {
            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(await GetStringFromPost(uri, postData));
            return doc;
        }

        internal async static Task<string> GetStringFromPost(Uri uri, IEnumerable<Tuple<string, string>> postData)
        {
            using (HttpWebResponse response = await Post(uri, postData))
            {
                using (Stream s = response.GetResponseStream())
                {
                    using (TextReader tr = new StreamReader(s))
                    {
                        return await tr.ReadToEndAsync();
                    }
                }
            }
        }

        internal async static Task<HttpWebResponse> Post(Uri uri, IEnumerable<Tuple<string, string>> postData)
        {
            HttpWebRequest request = WebRequest.CreateHttp(uri);
            request.Method = "POST";
            request.ContentType = "application/x-www-form-urlencoded";
            string postDataString = postData == null ? string.Empty : string.Join("&",
                postData.Select(d => string.Format("{0}={1}", d.Item1, HttpUtility.UrlEncode(d.Item2))));
            byte[] byteArray = Encoding.UTF8.GetBytes(postDataString);
            request.ContentLength = byteArray.Length;

            using (Stream requestStream = await request.GetRequestStreamAsync())
            {
                await requestStream.WriteAsync(byteArray, 0, byteArray.Length);
            }

            return (HttpWebResponse)await request.GetResponseAsync();
        }
    }
}
