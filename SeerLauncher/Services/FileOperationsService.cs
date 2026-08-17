using System;
using System.Diagnostics;
using System.IO;
using Microsoft.VisualBasic.FileIO;

namespace SeerLauncher.Services
{
    public class FileOperationsService
    {
        public bool Launch(string fullPath)
        {
            if (!File.Exists(fullPath)) return false;
            try
            {
                Process.Start(new ProcessStartInfo(fullPath) { UseShellExecute = true });
                return true;
            }
            catch
            {
                return false;
            }
        }

        public void OpenDirectory(string directory)
        {
            if (string.IsNullOrEmpty(directory) || !Directory.Exists(directory)) return;
            var args = directory.IndexOf(' ') >= 0 ? "\"" + directory + "\"" : directory;
            Process.Start(new ProcessStartInfo("explorer.exe", args) { UseShellExecute = true });
        }

        public bool DeleteToRecycleBin(string fullPath)
        {
            if (!File.Exists(fullPath)) return false;
            try
            {
                FileSystem.DeleteFile(fullPath, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static void OpenUrl(string url)
        {
            if (!IsSafeUrl(url)) return;
            try
            {
                Process.Start(url);
            }
            catch
            {
            }
        }

        public static bool IsSafeUrl(string url)
        {
            Uri uri;
            return Uri.TryCreate(url, UriKind.Absolute, out uri)
                && uri.Scheme == Uri.UriSchemeHttps
                && !string.IsNullOrEmpty(uri.Host)
                && string.IsNullOrEmpty(uri.UserInfo)
                && uri.IsDefaultPort;
        }
    }
}
