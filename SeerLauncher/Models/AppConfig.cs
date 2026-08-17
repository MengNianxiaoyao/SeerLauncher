using System.Collections.Generic;

namespace SeerLauncher.Models
{
    public class AppConfig
    {
        public List<string> Keywords { get; set; } = new List<string>();
        public Dictionary<string, string> Programs { get; set; } = new Dictionary<string, string>(System.StringComparer.OrdinalIgnoreCase);
    }
}
