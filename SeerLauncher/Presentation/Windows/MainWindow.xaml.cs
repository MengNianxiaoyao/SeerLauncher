using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using SeerLauncher.Functions.Programs;
using SeerLauncher.Functions.Updates;
using SeerLauncher.Infrastructure.Configuration;
using SeerLauncher.Infrastructure.FileSystem;
using SeerLauncher.Presentation;
using SeerLauncher.Presentation.Services;

namespace SeerLauncher.Presentation.Windows
{
    public partial class MainWindow : BaseWindow
    {
        private const int WmNcRButtonUp = 0x00A5;
        private const int HtCaption = 2;
        private const int WmDropFiles = 0x0233;
        private const int WmCopyData = 0x004A;
        private const int WmCopyGlobalData = 0x0049;
        private const uint MsgfltAllow = 1;

        private readonly MainViewModel _viewModel;

        public MainWindow()
        {
            InitializeComponent();
            var runDirectory = AppDomain.CurrentDomain.BaseDirectory;
            var selfName = Process.GetCurrentProcess().ProcessName + ".exe";
            _viewModel = new MainViewModel(
                new ConfigService(runDirectory),
                new ProgramScanService(),
                new UpdateService(Constants.UserAgent),
                new FileOperationsService(),
                new WpfUiService(),
                runDirectory,
                selfName);
            DataContext = _viewModel;
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            var handle = new WindowInteropHelper(this).Handle;
            // 提权后从资源管理器（普通权限）拖文件进来：UIPI 会拦截，需放行这三个消息
            // 0x0049 为 WM_COPYGLOBALDATA，跨完整性级别传输拖拽数据时使用
            ChangeWindowMessageFilterEx(handle, WmDropFiles, MsgfltAllow, IntPtr.Zero);
            ChangeWindowMessageFilterEx(handle, WmCopyData, MsgfltAllow, IntPtr.Zero);
            ChangeWindowMessageFilterEx(handle, WmCopyGlobalData, MsgfltAllow, IntPtr.Zero);
            DragAcceptFiles(handle, true);
            HwndSource.FromHwnd(handle).AddHook(WindowMessageHook);
        }

        private IntPtr WindowMessageHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WmNcRButtonUp && wParam.ToInt32() == HtCaption)
            {
                handled = true;
            }
            else if (msg == WmDropFiles)
            {
                handled = OnFilesDropped(wParam);
            }
            return IntPtr.Zero;
        }

        private bool OnFilesDropped(IntPtr hDrop)
        {
            try
            {
                var files = ReadDroppedFiles(hDrop);
                if (files.Length > 0) _viewModel.Programs.HandleDroppedFiles(files);
                return true;
            }
            finally
            {
                DragFinish(hDrop);
            }
        }

        private static string[] ReadDroppedFiles(IntPtr hDrop)
        {
            var count = (int)DragQueryFile(hDrop, uint.MaxValue, null, 0);
            var files = new string[count];
            for (uint index = 0; index < count; index++)
            {
                var length = (int)DragQueryFile(hDrop, index, null, 0);
                var buffer = new char[length + 1];
                DragQueryFile(hDrop, index, buffer, (uint)buffer.Length);
                files[index] = new string(buffer, 0, (int)length);
            }
            return files;
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool ChangeWindowMessageFilterEx(IntPtr hwnd, int message, uint action, IntPtr changeFilterStruct);

        [DllImport("shell32.dll", SetLastError = true)]
        private static extern void DragAcceptFiles(IntPtr hwnd, bool accept);

        [DllImport("shell32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern uint DragQueryFile(IntPtr hDrop, uint fileIndex, [Out] char[] fileName, uint cch);

        [DllImport("shell32.dll", SetLastError = true)]
        private static extern void DragFinish(IntPtr hDrop);

        private void ProgramsList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (ProgramsList.SelectedIndex >= 0 && _viewModel.Programs.LaunchCommand.CanExecute(null))
                _viewModel.Programs.LaunchCommand.Execute(null);
        }

        private void ProgramsList_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            var index = IndexUnderMouse(ProgramsList, e.GetPosition(ProgramsList));
            ProgramsList.SelectedIndex = index;
            var onItem = index >= 0;
            var menu = (ContextMenu)FindResource("ProgramsContextMenu");
            SetMenuItemVisibility(menu, "CtxRefreshProgram", true);
            SetMenuItemVisibility(menu, "CtxAddProgram", !onItem);
            SetMenuItemVisibility(menu, "CtxLaunchProgram", onItem);
            SetMenuItemVisibility(menu, "CtxDeleteProgram", onItem);
            SetMenuItemVisibility(menu, "CtxOpenDir", onItem);
        }

        private void KeywordList_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            var index = IndexUnderMouse(KeywordList, e.GetPosition(KeywordList));
            KeywordList.SelectedIndex = index;
            var onItem = index >= 0;
            var menu = (ContextMenu)FindResource("KeywordsContextMenu");
            SetMenuItemVisibility(menu, "CtxRefreshKeyword", true);
            SetMenuItemVisibility(menu, "CtxAddKeyword", !onItem);
            SetMenuItemVisibility(menu, "CtxModifyKeyword", onItem);
            SetMenuItemVisibility(menu, "CtxDeleteKeyword", onItem);
        }

        private void SetMenuItemVisibility(ContextMenu menu, string name, bool visible)
        {
            foreach (var item in menu.Items)
            {
                if (item is MenuItem mi && mi.Name == name)
                {
                    mi.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
                    return;
                }
            }
        }

        private static int IndexUnderMouse(ListBox list, Point position)
        {
            for (var i = 0; i < list.Items.Count; i++)
            {
                var container = list.ItemContainerGenerator.ContainerFromIndex(i) as ListBoxItem;
                if (container == null) continue;
                var topLeft = container.TranslatePoint(new Point(0, 0), list);
                if (position.X >= topLeft.X && position.Y >= topLeft.Y &&
                    position.X <= topLeft.X + container.ActualWidth &&
                    position.Y <= topLeft.Y + container.ActualHeight)
                    return i;
            }
            return -1;
        }

    }
}
