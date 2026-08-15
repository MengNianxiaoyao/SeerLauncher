using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Web.Script.Serialization;
using SeerLauncher.Models;

namespace SeerLauncher.Services
{
    public class ConfigService
    {
        public const string ConfigFileName = "SeerLauncher.json";
        public const string IniFileName = "SeerLauncher.ini";
        public const string IniBackupFileName = "SeerLauncher.ini.bak";

        private const string Separator = "|";
        private const string IniSectionConfig = "config";
        private const string IniKeyKeywords = "keywords";
        private const string IniSectionFilesName = "filesname";
        private const string IniKeyFilesName = "filesname";
        private const string IniSectionFileConfig = "fileconfig";

        private readonly JavaScriptSerializer _serializer = new JavaScriptSerializer();
        private readonly string _baseDirectory;
        
        private AppConfig _config;

        public ConfigService(string baseDirectory)
        {
            _baseDirectory = baseDirectory;
        }

        public string ConfigPath => Path.Combine(_baseDirectory, ConfigFileName);
        public string IniPath => Path.Combine(_baseDirectory, IniFileName);
        public string IniBackupPath => Path.Combine(_baseDirectory, IniBackupFileName);

        public AppConfig Config
        {
            get
            {
                if (_config == null) Load();
                return _config;
            }
        }

        public AppConfig Load()
        {
            if (File.Exists(ConfigPath))
            {
                try
                {
                    _config = Deserialize(File.ReadAllText(ConfigPath, Encoding.UTF8));
                }
                catch (Exception)
                {
                    _config = RecoverFromCorruptConfig();
                }
            }
            else if (File.Exists(IniPath))
            {
                _config = MigrateFromIni();
            }
            else
            {
                _config = CreateDefaultConfig();
            }
            return _config;
        }

        private AppConfig RecoverFromCorruptConfig()
        {
            if (File.Exists(IniBackupPath))
            {
                if (File.Exists(IniPath)) File.Delete(IniPath);
                File.Move(IniBackupPath, IniPath);
                return MigrateFromIni();
            }
            return CreateDefaultConfig();
        }

        private AppConfig CreateDefaultConfig()
        {
            var config = new AppConfig();
            foreach (var keyword in Split(Constants.DefaultKeywords))
                config.Keywords.Add(keyword);
            _config = config;
            Save();
            return config;
        }

        public void Save()
        {
            Directory.CreateDirectory(_baseDirectory);
            File.WriteAllText(ConfigPath, SerializeFormatted(_config), new UTF8Encoding(false));
        }

        private AppConfig Deserialize(string json)
        {
            var config = _serializer.Deserialize<AppConfig>(json);
            if (config.Keywords == null) config.Keywords = new List<string>();
            if (config.Programs == null) config.Programs = new Dictionary<string, string>();
            return config;
        }

        private AppConfig MigrateFromIni()
        {
            var config = new AppConfig();
            foreach (var keyword in Split(ReadIni(IniSectionConfig, IniKeyKeywords, "")))
                config.Keywords.Add(keyword);

            foreach (var name in Split(ReadIni(IniSectionFilesName, IniKeyFilesName, "")))
            {
                var path = ReadIni(IniSectionFileConfig, name, "");
                config.Programs[name] = path;
            }

            _config = config;
            Save();

            if (File.Exists(IniBackupPath)) File.Delete(IniBackupPath);
            File.Move(IniPath, IniBackupPath);
            return config;
        }

        public static List<string> Split(string text)
        {
            var list = new List<string>();
            if (string.IsNullOrEmpty(text)) return list;
            foreach (var item in text.Split(new[] { Separator }, StringSplitOptions.None))
                if (item.Length > 0) list.Add(item);
            return list;
        }

        public static bool IsValidKeyword(string keyword)
        {
            if (keyword == null) return false;
            const string illegal = "\\/:*?\"<>|";
            foreach (var c in illegal)
                if (keyword.IndexOf(c) >= 0) return false;
            return true;
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        private static extern int GetPrivateProfileString(string lpAppName, string lpKeyName, string lpDefault,
            StringBuilder lpReturnedString, int nSize, string lpFileName);

        private string ReadIni(string section, string key, string defaultValue)
        {
            var buffer = new StringBuilder(65536);
            GetPrivateProfileString(section, key, defaultValue, buffer, buffer.Capacity, IniPath);
            return buffer.ToString();
        }

        private string SerializeFormatted(AppConfig config)
        {
            var sb = new StringBuilder();
            sb.AppendLine("{");
            sb.AppendLine("  \"Keywords\": [");
            for (var i = 0; i < config.Keywords.Count; i++)
            {
                sb.Append("    \"");
                sb.Append(EscapeJson(config.Keywords[i]));
                sb.Append("\"");
                if (i < config.Keywords.Count - 1) sb.AppendLine(",");
                else sb.AppendLine();
            }
            sb.AppendLine("  ],");
            sb.AppendLine("  \"Programs\": {");
            var first = true;
            foreach (var kv in config.Programs)
            {
                if (!first) sb.AppendLine(",");
                first = false;
                sb.Append("    \"");
                sb.Append(EscapeJson(kv.Key));
                sb.Append("\": \"");
                sb.Append(EscapeJson(kv.Value));
                sb.Append("\"");
            }
            if (!config.Programs.Any()) sb.AppendLine();
            else sb.AppendLine();
            sb.AppendLine("  }");
            sb.Append("}");
            return sb.ToString();
        }

        private static string EscapeJson(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return s.Replace("\\", "\\\\")
                     .Replace("\"", "\\\"")
                     .Replace("\n", "\\n")
                     .Replace("\r", "\\r")
                     .Replace("\t", "\\t");
        }
    }
}