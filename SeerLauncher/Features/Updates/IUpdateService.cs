using System.Collections.Generic;
namespace SeerLauncher.Features.Updates
{
    public interface IUpdateService
    {
        UpdateInfo Fetch(string url);
        List<DownloadLink> FetchLinks(string url);
    }
}
