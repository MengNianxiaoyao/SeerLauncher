using System.Net;
using System.Windows;
using System.Windows.Threading;

namespace SeerLauncher
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Win7/部分老系统默认不启用 TLS 1.2，检查更新与脱机下载会握手失败
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;

            DispatcherUnhandledException += OnDispatcherUnhandledException;
        }

        private static void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            // 兜底：保存配置、启动程序等 IO 失败不再让程序直接退出
            e.Handled = true;
            MessageBox.Show(
                "程序遇到一个未处理的错误，但仍可继续使用。" + System.Environment.NewLine + e.Exception.Message,
                "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
