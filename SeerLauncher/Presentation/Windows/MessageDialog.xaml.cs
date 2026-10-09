using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace SeerLauncher.Presentation.Windows
{
    public enum UpdateChoice
    {
        Cancel,
        Global,
        Cn
    }

    public enum DeleteChoice
    {
        Cancel,
        RecycleBin,
        Permanent
    }

    public partial class MessageDialog : BaseWindow
    {
        private UpdateChoice _choice = UpdateChoice.Cancel;
        private DeleteChoice _deleteChoice = DeleteChoice.Cancel;

        private MessageDialog(string message, string caption, bool showCancel, bool isYesNo, bool isInfo, bool isUpdate, bool showCloseButton = true, bool isDelete = false)
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
            else if (isDelete)
            {
                AddDeleteButton("彻底删除", false, false, DeleteChoice.Permanent, 100, true);
                AddDeleteButton("移到回收站", true, false, DeleteChoice.RecycleBin, 100);
                AddDeleteButton("取消", false, true, DeleteChoice.Cancel);
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

        private void AddDeleteButton(string content, bool isDefault, bool isCancel, DeleteChoice choice, double width = 80, bool isDanger = false)
        {
            var btn = new Button
            {
                Content = content,
                Width = width,
                Height = 34,
                Margin = new Thickness(ButtonPanel.Children.Count == 0 ? 0 : 10, 0, 0, 0),
                IsDefault = isDefault,
                IsCancel = isCancel
            };
            if (isDanger) btn.Foreground = new SolidColorBrush(Color.FromRgb(0xC8, 0x1E, 0x1E));
            btn.Click += (s, e) => { _deleteChoice = choice; DialogResult = choice != DeleteChoice.Cancel; };
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

        public static DeleteChoice ShowDelete(string message, string caption = "删除程序")
        {
            var dialog = new MessageDialog(message, caption, false, false, false, false, true, true);
            dialog.Width = 440;
            return dialog.ShowDialog() == true ? dialog._deleteChoice : DeleteChoice.Cancel;
        }
    }
}
