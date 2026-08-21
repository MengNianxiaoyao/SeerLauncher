using System.Windows;
using System.Windows.Controls;

namespace SeerLauncher.Presentation.Windows
{
    public enum UpdateChoice
    {
        Cancel,
        Global,
        Cn
    }

    public partial class MessageDialog : BaseWindow
    {
        private UpdateChoice _choice = UpdateChoice.Cancel;

        private MessageDialog(string message, string caption, bool showCancel, bool isYesNo, bool isInfo, bool isUpdate, bool showCloseButton = true)
        {
            InitializeComponent();
            Title = caption;
            TitleBar.ShowCloseButton = showCloseButton;
            MessageText.Text = message;

            if (isInfo)
            {
                AddButton("确定", true, false);
            }
            else if (isYesNo)
            {
                AddButton("是", true, false);
                AddButton("否", false, true, dialogResult: false);
            }
            else if (isUpdate)
            {
                AddButton("GitHub", true, false, UpdateChoice.Global, 100);
                AddButton("网盘下载", false, true, UpdateChoice.Cn, 100);
            }
            else
            {
                AddButton("确定", true, false);
                if (showCancel)
                    AddButton("取消", false, true, dialogResult: false);
            }

            Owner = Application.Current.MainWindow;
        }

        private void AddButton(string content, bool isDefault, bool isCancel, UpdateChoice choice = UpdateChoice.Cancel, double width = 80, bool dialogResult = true)
        {
            var btn = new Button
            {
                Content = content,
                Width = width,
                Height = 34,
                Margin = new Thickness(isDefault ? 0 : 10, 0, 0, 0),
                IsDefault = isDefault,
                IsCancel = isCancel
            };
            btn.Click += (s, e) => { _choice = choice; DialogResult = dialogResult; };
            ButtonPanel.Children.Add(btn);
        }

        public static bool Show(string message, string caption = "操作提示", bool showCloseButton = true)
        {
            var dialog = new MessageDialog(message, caption, false, false, true, false, showCloseButton);
            return dialog.ShowDialog() == true;
        }

        public static bool Confirm(string message, string caption = "操作提示")
        {
            var dialog = new MessageDialog(message, caption, true, false, false, false);
            return dialog.ShowDialog() == true;
        }

        public static bool YesNo(string message, string caption = "操作提示")
        {
            var dialog = new MessageDialog(message, caption, false, true, false, false);
            return dialog.ShowDialog() == true;
        }

        public static UpdateChoice ShowUpdate(string message, string caption = "更新提示", bool showCloseButton = false)
        {
            var dialog = new MessageDialog(message, caption, false, false, false, true, showCloseButton);
            return dialog.ShowDialog() == true ? dialog._choice : UpdateChoice.Cancel;
        }
    }
}
