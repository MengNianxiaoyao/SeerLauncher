namespace SeerLauncher.Infrastructure.Configuration
{
    public interface IConfigService
    {
        bool HasCorruptConfig { get; }
        AppConfig Config { get; }
        AppConfig Load();
        bool Save();
        bool IsValidKeyword(string keyword);
    }
}
