using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using SeerLauncher.ViewModels;

namespace SeerLauncher
{
    public partial class MainWindow : Window
    {
        private const int WM_GETMINMAXINFO = 0x0024;

        private readonly MainViewModel _viewModel;

        public MainWindow()
        {
            InitializeComponent();
            _viewModel = new MainViewModel();
            DataContext = _viewModel;
            VersionBar.Text = _viewModel.VersionText;
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            var source = HwndSource.FromHwnd(new WindowInteropHelper(this).Handle);
            source?.AddHook(WndProc);
        }

        protected override void OnStateChanged(EventArgs e)
        {
            base.OnStateChanged(e);
            MaxBtn.Content = WindowState == WindowState.Maximized ? "❐" : "□";
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_GETMINMAXINFO)
            {
                var mmi = Marshal.PtrToStructure<MINMAXINFO>(lParam);
                var wa = SystemParameters.WorkArea;
                var scale = VisualTreeHelper.GetDpi(this).DpiScaleX;
                mmi.ptMaxPosition.X = (int)(wa.Left * scale);
                mmi.ptMaxPosition.Y = (int)(wa.Top * scale);
                mmi.ptMaxSize.X = (int)(wa.Width * scale);
                mmi.ptMaxSize.Y = (int)(wa.Height * scale);
                Marshal.StructureToPtr(mmi, lParam, true);
                handled = true;
            }
            return IntPtr.Zero;
        }

        private void MinimizeBtn_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;

        private void MaxBtn_Click(object sender, RoutedEventArgs e) =>
            WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;

        private void CloseBtn_Click(object sender, RoutedEventArgs e) => Close();

        private void ProgramsList_DragOver(object sender, DragEventArgs e)
        {
            e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop)
                ? DragDropEffects.Copy
                : DragDropEffects.None;
            e.Handled = true;
        }

        private void ProgramsList_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetData(DataFormats.FileDrop) is string[] files)
                _viewModel.HandleDroppedFiles(files);
        }

        private void ProgramsList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (ProgramsList.SelectedIndex >= 0)
                _viewModel.LaunchSelected();
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

        private void AddProgramMenu_Click(object sender, RoutedEventArgs e) => _viewModel.AddProgram();
        private void LaunchProgramMenu_Click(object sender, RoutedEventArgs e) => _viewModel.LaunchSelected();
        private void DeleteProgramMenu_Click(object sender, RoutedEventArgs e) => _viewModel.DeleteProgram();
        private void OpenDirMenu_Click(object sender, RoutedEventArgs e) => _viewModel.OpenDirectory();
        private void RefreshMenu_Click(object sender, RoutedEventArgs e) => _viewModel.RefreshDisplay();

        private void AddKeywordMenu_Click(object sender, RoutedEventArgs e) => _viewModel.AddKeyword();
        private void ModifyKeywordMenu_Click(object sender, RoutedEventArgs e) => _viewModel.ModifyKeyword();
        private void DeleteKeywordMenu_Click(object sender, RoutedEventArgs e) => _viewModel.DeleteKeyword();

        private void AuxDownload_Click(object sender, RoutedEventArgs e) => _viewModel.AuxiliaryDownload();
        private void CheckUpdate_Click(object sender, RoutedEventArgs e) => _viewModel.CheckUpdateFromButton();
        private void Instructions_Click(object sender, RoutedEventArgs e) => _viewModel.OpenInstructions();
        private void Developer_Click(object sender, RoutedEventArgs e) => _viewModel.OpenBilibili();
        private void Store_Click(object sender, RoutedEventArgs e) => _viewModel.OpenStore();
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct POINT
    {
        public int X;
        public int Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct MINMAXINFO
    {
        public POINT ptReserved;
        public POINT ptMaxSize;
        public POINT ptMaxPosition;
        public POINT ptMinTrackSize;
        public POINT ptMaxTrackSize;
    }
}