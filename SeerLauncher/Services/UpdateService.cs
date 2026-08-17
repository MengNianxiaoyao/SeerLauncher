using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using SeerLauncher.Models;

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
            return Parse(DownloadString(url));
        }

        public List<DownloadLink> FetchLinks(string url)
        {
            return ParseLinks(DownloadString(url));
        }

        private string DownloadString(string url)
        {
            using (var client = new WebClient())
            {
                client.Encoding = Encoding.UTF8;
                if (_userAgent.StartsWith("user-agent:", StringComparison.OrdinalIgnoreCase))
                    client.Headers[HttpRequestHeader.UserAgent] = _userAgent.Substring("user-agent:".Length).TrimStart();
                else
                    client.Headers[HttpRequestHeader.UserAgent] = _userAgent;
                return client.DownloadString(url);
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

        public static List<DownloadLink> ParseLinks(string text)
        {
            var list = new List<DownloadLink>();
            if (string.IsNullOrEmpty(text)) return list;
            var plain = Regex.Replace(text, @"<[^>]*>", string.Empty);
            foreach (Match m in Regex.Matches(plain, @"([^【\r\n]+?)【(https?://[^】\r\n]+)】\1"))
            {
                var name = m.Groups[1].Value.Trim();
                if (name.Length == 0 || IsUpdateField(name)) continue;
                list.Add(new DownloadLink { Name = name, Url = m.Groups[2].Value.Trim() });
            }
            return list;
        }

        private static bool IsUpdateField(string name)
        {
            switch (name)
            {
                case "下载链接":
                case "最新版本":
                case "强制更新":
                case "更新信息":
                    return true;
                default:
                    return false;
            }
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