using System;
using System.Windows;
using System.Windows.Input;
using SeerLauncher.Presentation.Controls;

namespace SeerLauncher.Presentation.Windows
{
    public abstract class BaseWindow : Window
    {
        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            WindowEffects.Apply(this);
        }

        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            // 屏蔽 Esc 防止对话框被误关；Alt 防止弹出系统菜单。
            // Tab 不能屏蔽：否则对话框内无法用键盘切换焦点（文本框 → 按钮）。
            if (e.Key == Key.Escape)
                e.Handled = true;

            if (e.Key == Key.System && (e.SystemKey == Key.LeftAlt || e.SystemKey == Key.RightAlt))
                e.Handled = true;
            base.OnPreviewKeyDown(e);
        }
    }
}
