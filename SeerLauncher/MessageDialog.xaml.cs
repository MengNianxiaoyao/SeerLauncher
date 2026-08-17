using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using SeerLauncher.Controls;

namespace SeerLauncher
{
    public partial class MessageDialog : Window
    {
        private MessageDialog(string message, string caption, bool showCancel, bool isYesNo, bool isInfo)
        {
            InitializeComponent();
            Title = caption;
            MessageText.Text = message;

            if (isInfo)
            {
                AddButton("确定", true, false);
            }
            else if (isYesNo)
            {
                AddButton("是", true, false);
                AddButton("否", false, true);
            }
            else
            {
                AddButton("确定", true, false);
                if (showCancel)
                    AddButton("取消", false, true);
            }

            Owner = Application.Current.MainWindow;
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

        private void AddButton(string content, bool isDefault, bool isCancel)
        {
            var btn = new Button
            {
                Content = content,
                Width = 80,
                Height = 34,
                Margin = new Thickness(isDefault ? 0 : 10, 0, 0, 0),
                IsDefault = isDefault,
                IsCancel = isCancel
            };
            btn.Click += (s, e) => { DialogResult = isDefault; };
            ButtonPanel.Children.Add(btn);
        }

        public static bool Show(string message, string caption = "操作提示")
        {
            var dialog = new MessageDialog(message, caption, false, false, true);
            return dialog.ShowDialog() == true;
        }

        public static bool Confirm(string message, string caption = "操作提示")
        {
            var dialog = new MessageDialog(message, caption, true, false, false);
            return dialog.ShowDialog() == true;
        }

        public static bool YesNo(string message, string caption = "操作提示")
        {
            var dialog = new MessageDialog(message, caption, false, true, false);
            return dialog.ShowDialog() == true;
        }
    }
}