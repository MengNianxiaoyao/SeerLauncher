namespace SeerLauncher.Infrastructure.Configuration
{
    public interface IConfigService
    {
        bool HasCorruptConfig { get; }
        AppConfig Config { get; }
        AppConfig Load();
        void Save();
        bool IsValidKeyword(string keyword);
    }
}
