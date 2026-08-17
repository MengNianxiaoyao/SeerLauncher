using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Win32;
using SeerLauncher.Models;
using SeerLauncher.ViewModels;

namespace SeerLauncher.Services
{
    public class WpfUiService : IUiService
    {
        private static Window Owner => Application.Current.MainWindow;

        public void ShowMessage(string message, string caption = "操作提示")
        {
            MessageDialog.Show(message, caption);
        }

        public bool Confirm(string message, string caption = "操作提示")
        {
            return MessageDialog.Confirm(message, caption);
        }

        public string Prompt(string prompt, string defaultValue, string title)
        {
            var dialog = new InputDialog(prompt, defaultValue, title) { Owner = Owner };
            return dialog.ShowDialog() == true ? dialog.InputText : null;
        }

        public string SelectExecutable()
        {
            var dialog = new OpenFileDialog
            {
                Filter = "可执行文件 (*.exe)|*.exe",
                Title = "选择程序"
            };
            return dialog.ShowDialog(Owner) == true ? dialog.FileName : null;
        }

        public void ShowDownloadLinks(IEnumerable<DownloadLink> links)
        {
            var viewModel = new DownloadDialogViewModel(links, this);
            new DownloadDialog(viewModel) { Owner = Owner }.ShowDialog();
        }

        public UpdateChoice ShowUpdate(string message, string caption, bool showCloseButton)
        {
            return MessageDialog.ShowUpdate(message, caption, showCloseButton);
        }

        public void OpenUrl(string url)
        {
            FileOperationsService.OpenUrl(url);
        }

        public void OpenDirectory(string path)
        {
            new FileOperationsService().OpenDirectory(path);
        }

        public void Shutdown()
        {
            Application.Current.Shutdown();
        }

        public void RunOnLoaded(Action action)
        {
            Application.Current.Dispatcher.BeginInvoke(action, DispatcherPriority.Loaded);
        }

        public void RunInBackground(Func<Task> action)
        {
            Application.Current.Dispatcher.BeginInvoke(new Action(async () => await RunSafely(action)),
                DispatcherPriority.Background);
        }

        private static async Task RunSafely(Func<Task> action)
        {
            try
            {
                await action();
            }
            catch
            {
            }
        }
    }
}
