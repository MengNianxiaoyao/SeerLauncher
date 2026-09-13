using System.Collections.Generic;
namespace SeerLauncher.Functions.Updates
{
    public interface IUpdateService
    {
        UpdateInfo Fetch(string url);
        List<DownloadLink> FetchLinks(string url);
    }
}
