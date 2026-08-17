using System.Windows;
using SeerLauncher.ViewModels;

namespace SeerLauncher
{
    public partial class DownloadDialog : BaseWindow
    {
        public DownloadDialog(DownloadDialogViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }

        private void OnClose(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
