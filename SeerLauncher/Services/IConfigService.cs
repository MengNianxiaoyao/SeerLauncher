using SeerLauncher.Models;

namespace SeerLauncher.Services
{
    public interface IConfigService
    {
        bool HasCorruptConfig { get; }
        AppConfig Config { get; }
        AppConfig Load();
        void Save();
    }
}
