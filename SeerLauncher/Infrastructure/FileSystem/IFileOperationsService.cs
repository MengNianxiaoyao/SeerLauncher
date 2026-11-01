namespace SeerLauncher.Infrastructure.FileSystem
{
    public interface IFileOperationsService
    {
        LaunchResult Launch(string fullPath);
        DeleteResult DeleteToRecycleBin(string fullPath);
        DeleteResult DeletePermanently(string fullPath);
    }
}
