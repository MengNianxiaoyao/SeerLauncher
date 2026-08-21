using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Web.Script.Serialization;
using SeerLauncher.Infrastructure.Configuration;

namespace SeerLauncher.Infrastructure.Configuration
{
    public class ConfigService : IConfigService
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

        public bool HasCorruptConfig { get; private set; }

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
            HasCorruptConfig = false;
            if (File.Exists(ConfigPath))
            {
                try
                {
                    _config = Deserialize(File.ReadAllText(ConfigPath, Encoding.UTF8));
                }
                catch (Exception)
                {
                    HasCorruptConfig = true;
                    _config = CreateDefaultConfig(false);
                }
            }
            else if (File.Exists(IniPath))
            {
                _config = MigrateFromIni();
            }
            else
            {
                _config = CreateDefaultConfig(true);
            }
            return _config;
        }

        private AppConfig CreateDefaultConfig(bool save)
        {
            var config = new AppConfig();
            foreach (var keyword in Constants.DefaultKeywords)
                config.Keywords.Add(keyword);
            _config = config;
            if (save) Save();
            return config;
        }

        public void Save()
        {
            Directory.CreateDirectory(_baseDirectory);
            var content = SerializeFormatted(_config);
            var tempPath = Path.Combine(_baseDirectory, ConfigFileName + "." + Guid.NewGuid().ToString("N") + ".tmp");
            try
            {
                File.WriteAllText(tempPath, content, new UTF8Encoding(false));
                if (File.Exists(ConfigPath))
                    File.Replace(tempPath, ConfigPath, null);
                else
                    File.Move(tempPath, ConfigPath);
            }
            finally
            {
                try
                {
                    if (File.Exists(tempPath)) File.Delete(tempPath);
                }
                catch
                {
                }
            }
        }

        private AppConfig Deserialize(string json)
        {
            var config = _serializer.Deserialize<AppConfig>(json);
            if (config.Keywords == null) config.Keywords = new List<string>();
            var programs = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (config.Programs != null)
            {
                foreach (var program in config.Programs)
                    programs[program.Key] = program.Value;
            }
            config.Programs = programs;
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

        public bool IsValidKeyword(string keyword)
        {
            if (string.IsNullOrWhiteSpace(keyword)) return false;
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
