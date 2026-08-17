namespace SeerLauncher.Services
{
    public interface IFileOperationsService
    {
        bool Launch(string fullPath);
        bool DeleteToRecycleBin(string fullPath);
    }
}
