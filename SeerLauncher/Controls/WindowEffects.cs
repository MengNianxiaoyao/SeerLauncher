using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using Microsoft.Win32;

namespace SeerLauncher.Controls
{
    internal static class WindowEffects
    {
        private const int DWMWA_WINDOW_CORNER_PREFERENCE = 33;
        private const int DWMWCP_ROUND = 2;
        private const int GWL_STYLE = -16;
        private const int WS_THICKFRAME = 0x00040000;

        [DllImport("dwmapi.dll", PreserveSig = false)]
        private static extern void DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hwnd, int index);

        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hwnd, int index, int value);

        public static void Apply(Window window)
        {
            var hwnd = new WindowInteropHelper(window).Handle;
            if (hwnd == IntPtr.Zero) return;

            // Ensure WS_THICKFRAME is set so DWM draws the full window shadow
            var style = GetWindowLong(hwnd, GWL_STYLE);
            if ((style & WS_THICKFRAME) == 0)
                SetWindowLong(hwnd, GWL_STYLE, style | WS_THICKFRAME);

            if (IsWindows11OrLater())
            {
                var corner = DWMWCP_ROUND;
                DwmSetWindowAttribute(hwnd, DWMWA_WINDOW_CORNER_PREFERENCE, ref corner, sizeof(int));
            }
        }

        private static bool IsWindows11OrLater()
        {
            var value = Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion",
                "CurrentBuildNumber", null);
            return value != null && int.TryParse(value.ToString(), out var build) && build >= 22000;
        }
    }
}