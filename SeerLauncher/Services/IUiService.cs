using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SeerLauncher.Models;

namespace SeerLauncher.Services
{
    public interface IUiService
    {
        void ShowMessage(string message, string caption = "操作提示");
        bool Confirm(string message, string caption = "操作提示");
        string Prompt(string prompt, string defaultValue, string title);
        string SelectExecutable();
        void ShowDownloadLinks(IEnumerable<DownloadLink> links);
        UpdateChoice ShowUpdate(string message, string caption, bool showCloseButton);
        void OpenUrl(string url);
        void OpenDirectory(string path);
        void Shutdown();
        void RunOnLoaded(Action action);
        void RunInBackground(Func<Task> action);
    }
}
