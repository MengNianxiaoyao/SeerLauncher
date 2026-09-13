using System.Windows;
using SeerLauncher.Functions.Updates;

namespace SeerLauncher.Presentation.Windows
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
