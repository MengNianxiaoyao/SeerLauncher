using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using SeerLauncher.Controls;
using SeerLauncher.Models;

namespace SeerLauncher
{
    public partial class DownloadDialog : Window
    {
        public DownloadDialog(IEnumerable<DownloadLink> links)
        {
            InitializeComponent();
            DataContext = links;
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            WindowEffects.Apply(this);
        }

        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            if (e.Key == Key.System && (e.SystemKey == Key.LeftAlt || e.SystemKey == Key.RightAlt))
                e.Handled = true;
            base.OnPreviewKeyDown(e);
        }

        private void Link_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Hyperlink link && link.NavigateUri != null)
            {
                try
                {
                    Process.Start(link.NavigateUri.ToString());
                }
                catch
                {
                }
                e.Handled = true;
            }
        }

        private void OnClose(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}