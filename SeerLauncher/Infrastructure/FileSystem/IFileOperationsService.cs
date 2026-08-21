namespace SeerLauncher.Infrastructure.FileSystem
{
    public interface IFileOperationsService
    {
        bool Launch(string fullPath);
        bool DeleteToRecycleBin(string fullPath);
    }
}
