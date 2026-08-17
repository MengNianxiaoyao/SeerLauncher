using System;
using System.Windows;
using SeerLauncher.Controls;

namespace SeerLauncher
{
    public partial class InputDialog : Window
    {
        public InputDialog(string prompt, string defaultValue = "")
        {
            InitializeComponent();
            PromptText.Text = prompt;
            InputBox.Text = defaultValue;
            InputBox.SelectAll();
            Loaded += (s, e) => InputBox.Focus();
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            WindowEffects.Apply(this);
        }

        public string InputText => InputBox.Text;

        private void OnOk(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        private void OnCancel(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}