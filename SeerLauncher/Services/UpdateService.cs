using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Web.Script.Serialization;
using SeerLauncher.Models;

namespace SeerLauncher.Services
{
    public class UpdateInfo
    {
        public string Version { get; set; }
        public string GlobalUrl { get; set; }
        public string CnUrl { get; set; }
        public string Info { get; set; }
        public bool IsForceUpdate { get; set; }
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

        public UpdateInfo Parse(string json)
        {
            var data = ParseJson(json);
            return new UpdateInfo
            {
                Version = data.Version,
                GlobalUrl = CleanUrl(data.CheckLink?.Global),
                CnUrl = CleanUrl(data.CheckLink?.Cn),
                Info = data.Info,
                IsForceUpdate = data.Force
            };
        }

        public static List<DownloadLink> ParseLinks(string json)
        {
            var list = new List<DownloadLink>();
            UpdateJson data;
            try
            {
                data = ParseJson(json);
            }
            catch
            {
                return list;
            }
            if (data.Tools == null) return list;
            foreach (var kv in data.Tools)
            {
                if (string.IsNullOrWhiteSpace(kv.Key) || string.IsNullOrWhiteSpace(kv.Value)) continue;
                list.Add(new DownloadLink { Name = kv.Key.Trim(), Url = CleanUrl(kv.Value) });
            }
            return list;
        }

        private static UpdateJson ParseJson(string json)
        {
            var outer = new JavaScriptSerializer().Deserialize<Dictionary<string, object>>(json);
            if (outer == null || !outer.TryGetValue("content", out var value))
                throw new InvalidOperationException("content not found");
            var plain = Regex.Replace(Convert.ToString(value), @"<[^>]*>", string.Empty);
            var content = WebUtility.HtmlDecode(plain).Trim();
            return new JavaScriptSerializer().Deserialize<UpdateJson>(content);
        }

        private static string CleanUrl(string url)
        {
            if (string.IsNullOrEmpty(url)) return url;
            return url.Replace("\u00A0", string.Empty).Replace(" ", string.Empty);
        }

        private class UpdateJson
        {
            public UpdateJson() { }

            public CheckLinkJson CheckLink { get; set; }
            public string Version { get; set; }
            public bool Force { get; set; }
            public string Info { get; set; }
            public Dictionary<string, string> Tools { get; set; }
        }

        private class CheckLinkJson
        {
            public CheckLinkJson() { }

            public string Global { get; set; }
            public string Cn { get; set; }
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