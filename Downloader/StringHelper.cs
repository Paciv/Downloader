using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Downloader
{
    internal static class StringHelper
    {
        public static string EndSubString(this string source, char start, char end, bool include = false)
        {
            int a = source.LastIndexOf(start);
            int b = source.LastIndexOf(end);
            if (!include)
            {
                a++;
                b--;
            }
            return source.Substring(a, b - a + 1);
        }

        public static string GetHumanReadableFileSize(this long size)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            int order = 0;
            while (size >= 1024 && order + 1 < sizes.Length)
            {
                order++;
                size = size / 1024;
            }

            return String.Format("{0:0.###}{1}", size, sizes[order]);
        }
    }
}
