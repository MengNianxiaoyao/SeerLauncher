using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using SeerLauncher.Presentation.Windows;

namespace SeerLauncher.Presentation.Controls
{
    public static class TextBoxClipboard
    {
        public static readonly DependencyProperty EnableProperty =
            DependencyProperty.RegisterAttached(
                "Enable",
                typeof(bool),
                typeof(TextBoxClipboard),
                new PropertyMetadata(false, OnEnableChanged));

        public static bool GetEnable(DependencyObject target)
        {
            return (bool)target.GetValue(EnableProperty);
        }

        public static void SetEnable(DependencyObject target, bool value)
        {
            target.SetValue(EnableProperty, value);
        }

        private static void OnEnableChanged(DependencyObject target, DependencyPropertyChangedEventArgs e)
        {
            var textBox = target as TextBox;
            if (textBox == null)
            {
                return;
            }

            if ((bool)e.NewValue)
            {
                Attach(textBox);
            }
            else
            {
                Detach(textBox);
            }
        }

        private static void Attach(TextBox textBox)
        {
            textBox.CommandBindings.Add(new CommandBinding(ApplicationCommands.Cut, OnCutExecuted, OnEditCanExecute));
            textBox.CommandBindings.Add(new CommandBinding(ApplicationCommands.Copy, OnCopyExecuted, OnEditCanExecute));
            textBox.CommandBindings.Add(new CommandBinding(ApplicationCommands.Paste, OnPasteExecuted, OnPasteCanExecute));
            EnsureContextMenu(textBox);
        }

        private static void Detach(TextBox textBox)
        {
            for (var index = textBox.CommandBindings.Count - 1; index >= 0; index--)
            {
                var binding = textBox.CommandBindings[index];
                if (binding.Command == ApplicationCommands.Cut ||
                    binding.Command == ApplicationCommands.Copy ||
                    binding.Command == ApplicationCommands.Paste)
                {
                    textBox.CommandBindings.RemoveAt(index);
                }
            }

            textBox.ClearValue(FrameworkElement.ContextMenuProperty);
        }

        private static void EnsureContextMenu(TextBox textBox)
        {
            if (textBox.ContextMenu != null)
            {
                return;
            }

            // 使用 App.xaml 中的全局 ContextMenu/MenuItem 隐式样式，不再另设专用样式
            var menu = new ContextMenu();
            menu.Items.Add(CreateMenuItem("剪切", ApplicationCommands.Cut, "Ctrl+X", textBox));
            menu.Items.Add(CreateMenuItem("复制", ApplicationCommands.Copy, "Ctrl+C", textBox));
            menu.Items.Add(CreateMenuItem("粘贴", ApplicationCommands.Paste, "Ctrl+V", textBox));
            textBox.ContextMenu = menu;
        }

        private static MenuItem CreateMenuItem(string header, RoutedUICommand command, string gesture, TextBox target)
        {
            var item = new MenuItem();
            item.Header = header;
            item.Command = command;
            item.CommandTarget = target;
            item.InputGestureText = gesture;
            return item;
        }

        private static bool IsEditable(TextBox textBox)
        {
            return textBox != null && textBox.IsEnabled && !textBox.IsReadOnly;
        }

        private static void OnEditCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            var textBox = sender as TextBox;
            e.CanExecute = IsEditable(textBox) && textBox.SelectionLength > 0;
            e.Handled = true;
        }

        private static void OnPasteCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = IsEditable(sender as TextBox);
            e.Handled = true;
        }

        private static async void OnCutExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            e.Handled = true;
            var textBox = sender as TextBox;
            if (!IsEditable(textBox))
            {
                return;
            }

            if (textBox.SelectionLength == 0)
            {
                return;
            }

            var selectedText = textBox.SelectedText;
            bool written;
            try
            {
                written = await TrySetTextAsync(selectedText);
            }
            catch (InvalidOperationException)
            {
                written = false;
            }

            if (!written)
            {
                ShowClipboardBusy("剪切");
                return;
            }

            if (!IsEditable(textBox) || textBox.SelectedText != selectedText)
            {
                return;
            }

            textBox.SelectedText = string.Empty;
        }

        private static async void OnCopyExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            e.Handled = true;
            var textBox = sender as TextBox;
            if (!IsEditable(textBox))
            {
                return;
            }

            if (textBox.SelectionLength == 0)
            {
                return;
            }

            var selectedText = textBox.SelectedText;
            bool written;
            try
            {
                written = await TrySetTextAsync(selectedText);
            }
            catch (InvalidOperationException)
            {
                written = false;
            }

            if (!written)
            {
                ShowClipboardBusy("复制");
            }
        }

        private static void OnPasteExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            e.Handled = true;
            var textBox = sender as TextBox;
            if (!IsEditable(textBox))
            {
                return;
            }

            string text;
            try
            {
                text = Clipboard.ContainsText() ? Clipboard.GetText() : null;
            }
            catch (ExternalException)
            {
                ShowClipboardBusy("粘贴");
                return;
            }

            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            var insertionStart = textBox.SelectionStart;
            textBox.SelectedText = text;
            textBox.Select(insertionStart + text.Length, 0);
        }

        public static bool TrySetText(string text)
        {
            // 纯原生单步抢占：OpenClipboard 独占，抢到即拥有；后台线程每 10ms 抢一次，最多约 10 秒
            // SetClipboardData 成功即权威成功，不做托管读回（后台线程无消息循环，托管调用不可靠）
            for (var attempt = 0; attempt < 1000; attempt++)
            {
                if (TryOpenAndSet(text))
                {
                    return true;
                }

                Thread.Sleep(10);
            }

            return false;
        }

        private static bool TryOpenAndSet(string text)
        {
            if (!OpenClipboard(IntPtr.Zero))
            {
                return false;
            }

            try
            {
                if (!EmptyClipboard())
                {
                    return false;
                }

                var handle = GlobalAlloc(GmemMoveable | GmemZeroInit, (UIntPtr)(uint)((text.Length + 1) * 2));
                if (handle == IntPtr.Zero)
                {
                    return false;
                }

                var pointer = GlobalLock(handle);
                if (pointer == IntPtr.Zero)
                {
                    GlobalFree(handle);
                    return false;
                }

                try
                {
                    Marshal.Copy(text.ToCharArray(), 0, pointer, text.Length);
                }
                finally
                {
                    GlobalUnlock(handle);
                }

                if (SetClipboardData(CfUnicodeText, handle) == IntPtr.Zero)
                {
                    GlobalFree(handle);
                    return false;
                }

                return true;
            }
            finally
            {
                CloseClipboard();
            }
        }

        private const uint CfUnicodeText = 13;
        private const uint GmemMoveable = 0x0002;
        private const uint GmemZeroInit = 0x0040;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool OpenClipboard(IntPtr hWndNewOwner);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool CloseClipboard();

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool EmptyClipboard();

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SetClipboardData(uint uFormat, IntPtr hMem);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GlobalAlloc(uint uFlags, UIntPtr dwBytes);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GlobalLock(IntPtr hMem);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool GlobalUnlock(IntPtr hMem);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GlobalFree(IntPtr hMem);

        public static Task<bool> TrySetTextAsync(string text)
        {
            var completion = new TaskCompletionSource<bool>();
            var thread = new Thread(() =>
            {
                try
                {
                    completion.SetResult(TrySetText(text));
                }
                catch (Exception exception)
                {
                    completion.SetException(exception);
                }
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.IsBackground = true;
            thread.Start();
            return completion.Task;
        }

        private static void ShowClipboardBusy(string caption)
        {
            MessageDialog.Show("剪贴板正被其他程序占用，请稍后重试。", caption);
        }
    }
}
