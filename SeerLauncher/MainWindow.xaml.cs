using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using SeerLauncher.Services;
using SeerLauncher.ViewModels;

namespace SeerLauncher
{
    public partial class MainWindow : BaseWindow
    {
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
            if (ProgramsList.SelectedIndex >= 0 && _viewModel.LaunchProgramCommand.CanExecute(null))
                _viewModel.LaunchProgramCommand.Execute(null);
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
