using System.Collections.Generic;
using SeerLauncher.Models;

namespace SeerLauncher.Services
{
    public interface IUpdateService
    {
        UpdateInfo Fetch(string url);
        List<DownloadLink> FetchLinks(string url);
    }
}
