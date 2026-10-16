using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using Microsoft.Win32;

namespace SeerLauncher.Presentation.Controls
{
    internal static class WindowEffects
    {
        private const int DWMWA_WINDOW_CORNER_PREFERENCE = 33;
        private const int DWMWCP_ROUND = 2;
        private const int GWL_STYLE = -16;
        private const int WS_THICKFRAME = 0x00040000;

        // 检测结果缓存：注册表只读一次，避免每个窗口重复查
        private static readonly bool IsWindows11OrLater = DetectWindows11OrLater();

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hwnd, int index);

        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hwnd, int index, int value);

        public static void Apply(Window window)
        {
            var hwnd = new WindowInteropHelper(window).Handle;
            if (hwnd == IntPtr.Zero) return;

            // Win10/Win11 通用：补 WS_THICKFRAME，DWM 才会画完整窗口阴影
            //（WindowStyle=None 会丢掉该样式，无边框窗口必须手动补回）
            var style = GetWindowLong(hwnd, GWL_STYLE);
            if ((style & WS_THICKFRAME) == 0)
                SetWindowLong(hwnd, GWL_STYLE, style | WS_THICKFRAME);

            // Win11 专属：DWMWA_WINDOW_CORNER_PREFERENCE（attr 33）在 Win10 上不存在，
            // 必须先分支、再吞错（精简版 DWM / 老系统直接忽略，不许崩启动）。
            // Win10 不做任何额外处理：方形直角 + 上一步的阴影即原生观感，
            // 不要在 WPF 内容里强行画圆角，否则与 DWM 边框错位露角。
            if (IsWindows11OrLater)
            {
                try
                {
                    var corner = DWMWCP_ROUND;
                    DwmSetWindowAttribute(hwnd, DWMWA_WINDOW_CORNER_PREFERENCE, ref corner, sizeof(int));
                }
                catch (TypeLoadException)
                {
                    // dwmapi 缺失或无该入口（老系统/精简版 DWM）：忽略，只不要圆角
                }
            }
        }

        private static bool DetectWindows11OrLater()
        {
            try
            {
                var value = Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion",
                    "CurrentBuildNumber", null);
                return value != null && int.TryParse(value.ToString(), out var build) && build >= 22000;
            }
            catch (Exception)
            {
                // 注册表不可读时按 Win10 对待：只保证阴影，不碰圆角，最安全
                return false;
            }
        }
    }
}
