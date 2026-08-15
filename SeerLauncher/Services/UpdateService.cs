using System;
using System.Net;
using System.Text;

namespace SeerLauncher.Services
{
    public class UpdateInfo
    {
        public string Version { get; set; }
        public string DownloadUrl { get; set; }
        public string Info { get; set; }
        public string ForceUpdate { get; set; }

        public bool IsForceUpdate =>
            string.Equals(ForceUpdate, "是", StringComparison.Ordinal);
    }

    public class UpdateService
    {
        private readonly string _userAgent;

        public UpdateService(string userAgent)
        {
            _userAgent = userAgent;
        }

        public UpdateInfo Fetch(string url)
        {
            using (var client = new WebClient())
            {
                client.Encoding = Encoding.UTF8;
                if (_userAgent.StartsWith("user-agent:", StringComparison.OrdinalIgnoreCase))
                    client.Headers[HttpRequestHeader.UserAgent] = _userAgent.Substring("user-agent:".Length).TrimStart();
                else
                    client.Headers[HttpRequestHeader.UserAgent] = _userAgent;
                var html = client.DownloadString(url);
                return Parse(html);
            }
        }

        public UpdateInfo Parse(string html)
        {
            return new UpdateInfo
            {
                Version = Extract(html, "最新版本【", "】最新版本"),
                DownloadUrl = Extract(html, "下载链接【", "】下载链接"),
                Info = Extract(html, "更新信息【", "】更新信息"),
                ForceUpdate = Extract(html, "强制更新【", "】强制更新")
            };
        }

        public static string Extract(string text, string start, string end)
        {
            if (string.IsNullOrEmpty(text)) return "";
            var startIndex = text.IndexOf(start, StringComparison.Ordinal);
            if (startIndex < 0) return "";
            startIndex += start.Length;
            var endIndex = text.IndexOf(end, startIndex, StringComparison.Ordinal);
            if (endIndex < 0) return "";
            return text.Substring(startIndex, endIndex - startIndex);
        }

        public static int ToVersionInt(string version)
        {
            var parts = (version ?? "").Split('.');
            return Part(0) * 10000 + Part(1) * 100 + Part(2);

            int Part(int index)
            {
                return index < parts.Length && int.TryParse(parts[index], out var v) ? v : 0;
            }
        }

        public static bool IsNewer(string newVersion, string currentVersion)
        {
            return ToVersionInt(newVersion) > ToVersionInt(currentVersion);
        }
    }
}