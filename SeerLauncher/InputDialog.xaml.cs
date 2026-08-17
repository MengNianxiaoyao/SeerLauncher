using System.Windows;

namespace SeerLauncher
{
    public partial class InputDialog : BaseWindow
    {
        public InputDialog(string prompt, string defaultValue = "", string title = "输入")
        {
            InitializeComponent();
            Title = title;
            PromptText.Text = prompt;
            InputBox.Text = defaultValue;
            InputBox.SelectAll();
            Loaded += (s, e) => InputBox.Focus();
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