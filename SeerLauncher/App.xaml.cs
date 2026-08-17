using System;
using System.Threading.Tasks;
using System.Windows;
using SeerLauncher.Services;

namespace SeerLauncher
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            if (CheckUpdateAtStartup())
            {
                Shutdown();
                return;
            }
            var window = new MainWindow();
            MainWindow = window;
            window.Show();
        }

        private static bool CheckUpdateAtStartup()
        {
            var updater = new UpdateService(Constants.UserAgent);
            UpdateInfo info;
            try
            {
                var fetchTask = Task.Run(() => updater.Fetch(Constants.CheckUrl));
                if (!fetchTask.Wait(TimeSpan.FromSeconds(5))) return false;
                info = fetchTask.Result;
            }
            catch
            {
                return false;
            }
            return UpdatePrompter.PromptAndShouldExit(info, false);
        }
    }
}