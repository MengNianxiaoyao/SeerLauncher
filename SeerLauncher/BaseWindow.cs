using System;
using System.Windows;
using System.Windows.Input;
using SeerLauncher.Controls;

namespace SeerLauncher
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
            if (e.Key == Key.Escape || e.Key == Key.Tab)
                e.Handled = true;

            if (e.Key == Key.System && (e.SystemKey == Key.LeftAlt || e.SystemKey == Key.RightAlt))
                e.Handled = true;
            base.OnPreviewKeyDown(e);
        }
    }
}