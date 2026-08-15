using System;
using System.Collections.Generic;
using System.IO;

namespace SeerLauncher.Services
{
    public class ProgramScanService
    {
        public List<string> Scan(string directory, string selfName, IList<string> keywords)
        {
            var result = new List<string>();
            if (!Directory.Exists(directory)) return result;

            foreach (var file in Directory.GetFiles(directory, "*.exe", SearchOption.TopDirectoryOnly))
            {
                var name = Path.GetFileName(file);
                if (string.Equals(name, selfName, StringComparison.OrdinalIgnoreCase)) continue;

                foreach (var keyword in keywords)
                {
                    if (string.IsNullOrEmpty(keyword)) continue;
                    if (name.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        result.Add(name);
                        break;
                    }
                }
            }
            return result;
        }

        public List<string> MergeConfiguredAndScanned(IList<string> configured, IList<string> scanned)
        {
            var result = new List<string>(configured);
            foreach (var item in scanned)
                if (!result.Contains(item))
                    result.Add(item);
            return result;
        }
    }
}