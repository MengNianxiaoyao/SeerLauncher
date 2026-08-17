using System.Collections.Generic;
using System.Windows;
using System.Windows.Documents;
using SeerLauncher.Models;
using SeerLauncher.Services;

namespace SeerLauncher
{
    public partial class DownloadDialog : BaseWindow
    {
        public DownloadDialog(IEnumerable<DownloadLink> links)
        {
            InitializeComponent();
            DataContext = links;
        }

        private void Link_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Hyperlink link && link.NavigateUri != null)
            {
                FileOperationsService.OpenUrl(link.NavigateUri.ToString());
                e.Handled = true;
            }
        }

        private void OnClose(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}