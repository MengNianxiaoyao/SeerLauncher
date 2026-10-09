using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Input;

namespace SeerLauncher.Presentation.Windows
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

        private bool IsEditable => InputBox != null && InputBox.IsEnabled && !InputBox.IsReadOnly;

        private void OnEditCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = IsEditable && InputBox.SelectionLength > 0;
            e.Handled = true;
        }

        private void OnPasteCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = IsEditable;
            e.Handled = true;
        }

        private void OnCutExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            e.Handled = true;
            if (InputBox.SelectionLength == 0) return;
            if (!WriteClipboard(InputBox.SelectedText, "剪切")) return;
            InputBox.SelectedText = string.Empty;
        }

        private void OnCopyExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            e.Handled = true;
            if (InputBox.SelectionLength == 0) return;
            WriteClipboard(InputBox.SelectedText, "复制");
        }

        private void OnPasteExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            e.Handled = true;
            string text;
            try
            {
                text = Clipboard.ContainsText() ? Clipboard.GetText() : null;
            }
            catch (ExternalException)
            {
                MessageDialog.Show("剪贴板正被其他程序占用，粘贴失败，请稍后重试。", "粘贴");
                return;
            }
            if (string.IsNullOrEmpty(text)) return;
            InputBox.SelectedText = text;
        }

        private static bool WriteClipboard(string text, string caption)
        {
            // SetText 按 copy:true 语义写入（含落盘）。其他程序可能长时间持有剪贴板，
            // 退避重试，总预算约 1 秒；耗尽才认定失败。
            int[] delays = { 50, 100, 200, 400 };
            foreach (int delay in delays)
            {
                try
                {
                    Clipboard.SetText(text);
                    return true;
                }
                catch (ExternalException)
                {
                    if (IsClipboardText(text)) return true;
                    Thread.Sleep(delay);
                }
            }
            MessageDialog.Show("剪贴板正被其他程序占用，请稍后重试。", caption);
            return false;
        }

        private static bool IsClipboardText(string expected)
        {
            try
            {
                return Clipboard.ContainsText() && Clipboard.GetText() == expected;
            }
            catch (ExternalException)
            {
                return false;
            }
        }
    }
}
