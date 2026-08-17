using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using SeerLauncher.Mvvm;
using SeerLauncher.Services;

namespace SeerLauncher.ViewModels
{
    public class MainViewModel : ObservableObject
    {
        private readonly ConfigService _configService;
        private readonly ProgramScanService _scanner = new ProgramScanService();
        private readonly UpdateService _updater;
        private readonly FileOperationsService _fileOps = new FileOperationsService();
        private readonly string _runDirectory;
        private readonly string _selfName;
        private static Window OwnerWin => Application.Current.MainWindow;

        public MainViewModel()
        {
            _runDirectory = AppDomain.CurrentDomain.BaseDirectory;
            _selfName = System.Diagnostics.Process.GetCurrentProcess().ProcessName + ".exe";
            _configService = new ConfigService(_runDirectory);
            _updater = new UpdateService(Constants.UserAgent);

            var config = _configService.Load();
            foreach (var keyword in config.Keywords) Keywords.Add(keyword);
            RefreshDisplay();

            AddKeywordCommand = new RelayCommand(AddKeyword);
            ModifyKeywordCommand = new RelayCommand(ModifyKeyword, () => SelectedKeyword != null);
            DeleteKeywordCommand = new RelayCommand(DeleteKeyword, () => SelectedKeyword != null);
            AddProgramCommand = new RelayCommand(AddProgram);
            LaunchProgramCommand = new RelayCommand(LaunchSelected, () => SelectedProgram != null);
            DeleteProgramCommand = new RelayCommand(DeleteProgram, () => SelectedProgram != null);
            OpenDirectoryCommand = new RelayCommand(OpenDirectory, () => SelectedProgram != null);
            RefreshDisplayCommand = new RelayCommand(RefreshDisplay);

            Application.Current.Dispatcher.BeginInvoke(new Action(() => CheckUpdateAsync(false)), DispatcherPriority.Background);
        }

        public ObservableCollection<string> Keywords { get; } = new ObservableCollection<string>();
        public ObservableCollection<string> Programs { get; } = new ObservableCollection<string>();

        public string VersionText => "Ver: " + Constants.CurrentVersion;

        private string _selectedKeyword;
        public string SelectedKeyword
        {
            get { return _selectedKeyword; }
            set { SetProperty(ref _selectedKeyword, value); }
        }

        private string _selectedProgram;
        public string SelectedProgram
        {
            get { return _selectedProgram; }
            set { SetProperty(ref _selectedProgram, value); }
        }

        public ICommand AddKeywordCommand { get; }
        public ICommand ModifyKeywordCommand { get; }
        public ICommand DeleteKeywordCommand { get; }
        public ICommand AddProgramCommand { get; }
        public ICommand LaunchProgramCommand { get; }
        public ICommand DeleteProgramCommand { get; }
        public ICommand OpenDirectoryCommand { get; }
        public ICommand RefreshDisplayCommand { get; }

        public void RefreshDisplay()
        {
            var config = _configService.Config;
            var configured = new List<string>(config.Programs.Keys);
            var scanned = _scanner.Scan(_runDirectory, _selfName, config.Keywords);

            Keywords.Clear();
            foreach (var keyword in config.Keywords) Keywords.Add(keyword);

            Programs.Clear();
            foreach (var item in _scanner.MergeConfiguredAndScanned(configured, scanned))
                Programs.Add(item);
        }

        public void AddKeyword()
        {
            var dialog = new InputDialog("请输入要添加的关键字", title: "添加关键字") { Owner = OwnerWin };
            if (dialog.ShowDialog() != true) return;

            var keyword = dialog.InputText;
            if (string.IsNullOrEmpty(keyword))
            {
                MessageDialog.Show("添加的关键字不能为空", "操作提示");
                return;
            }
            if (!ConfigService.IsValidKeyword(keyword))
            {
                MessageDialog.Show("关键字不能包含下列任何字符：" + Environment.NewLine + "\\/:*?\"" + "<>|", "操作提示");
                return;
            }
            _configService.Config.Keywords.Add(keyword.Trim());
            _configService.Save();
            RefreshDisplay();
        }

        public void ModifyKeyword()
        {
            if (SelectedKeyword == null)
            {
                MessageDialog.Show("请选择需要修改的关键字", "操作提示");
                return;
            }
            var dialog = new InputDialog("请输入新的关键字", SelectedKeyword, "修改关键字") { Owner = OwnerWin };
            if (dialog.ShowDialog() != true) return;

var keyword = dialog.InputText;
            if (string.IsNullOrEmpty(keyword))
            {
                MessageDialog.Show("新的关键字不能为空", "操作提示");
                return;
            }
            if (!ConfigService.IsValidKeyword(keyword))
            {
                MessageDialog.Show("关键字不能包含下列任何字符：" + Environment.NewLine + "\\/:*?\"" + "<>|", "操作提示");
                return;
            }
            var index = _configService.Config.Keywords.IndexOf(SelectedKeyword);
            if (index >= 0) _configService.Config.Keywords[index] = keyword;
            _configService.Save();
            RefreshDisplay();
        }

        public void DeleteKeyword()
        {
            if (SelectedKeyword == null)
            {
                MessageDialog.Show("请选择需要删除的关键字", "操作提示");
                return;
            }
            if (!MessageDialog.Confirm("是否删除此关键字？")) return;

            _configService.Config.Keywords.Remove(SelectedKeyword);
            _configService.Save();
            RefreshDisplay();
        }

        public void AddProgram()
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "可执行文件 (*.exe)|*.exe",
                Title = "选择程序"
            };
            if (dialog.ShowDialog() == true)
                AddProgramFile(dialog.FileName);
        }

        public void AddProgramFile(string fullPath)
        {
            var name = Path.GetFileName(fullPath);
            if (!name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                name += ".exe";
            var directory = Path.GetDirectoryName(fullPath) ?? "";

            if (_configService.Config.Programs.ContainsKey(name))
            {
                _configService.Config.Programs[name] = directory;
                _configService.Save();
                return;
            }

            _configService.Config.Programs[name] = directory;
            _configService.Save();
            RefreshDisplay();
        }

        public void HandleDroppedFiles(IEnumerable<string> files)
        {
            foreach (var file in files)
            {
                var name = Path.GetFileName(file);
                if (!name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                {
                    MessageDialog.Show("添加的文件不是可执行文件", "操作提示");
                    continue;
                }
                if (MessageDialog.Confirm("是否添加《" + name + "》？"))
                {
                    AddProgramFile(file);
                }
            }
        }

        public void LaunchSelected()
        {
            if (SelectedProgram == null)
            {
                MessageDialog.Show("请选择要启动的程序", "操作提示");
                return;
            }
            var name = SelectedProgram;
            string path;
            _configService.Config.Programs.TryGetValue(name, out path);
            var fullPath = string.IsNullOrEmpty(path)
                ? Path.Combine(_runDirectory, name)
                : Path.Combine(path, name);
            if (!_fileOps.Launch(fullPath))
                MessageDialog.Show("启动失败，文件不存在：" + fullPath, "操作提示");
        }

        public void DeleteProgram()
        {
            if (SelectedProgram == null)
            {
                MessageDialog.Show("请选择要删除的程序", "操作提示");
                return;
            }
            var name = SelectedProgram;
            if (!MessageDialog.Confirm("是否将《" + name + "》移动到回收站？")) return;

            string path;
            _configService.Config.Programs.TryGetValue(name, out path);
            var fullPath = string.IsNullOrEmpty(path)
                ? Path.Combine(_runDirectory, name)
                : Path.Combine(path, name);

            _fileOps.DeleteToRecycleBin(fullPath);
            _configService.Config.Programs.Remove(name);
            _configService.Save();
            RefreshDisplay();
        }

        public void OpenDirectory()
        {
            if (SelectedProgram == null)
            {
                _fileOps.OpenDirectory(_runDirectory);
                return;
            }
            var name = SelectedProgram;
            string path;
            _configService.Config.Programs.TryGetValue(name, out path);
            _fileOps.OpenDirectory(string.IsNullOrEmpty(path) ? _runDirectory : path);
        }

        public void AuxiliaryDownload()
        {
            if (MessageDialog.Confirm("点击确认跳转至开发者博客，点击取消跳转至有道云文档", "跳转提示"))
                OpenUrl(Constants.DeveloperBlog);
            else
                OpenUrl(Constants.YoudaoDocs);
        }

        public void OpenInstructions() => OpenUrl(Constants.InstructionsUrl);
        public void OpenBilibili() => OpenUrl(Constants.DeveloperBilibili);
        public void OpenStore() => OpenUrl(Constants.StoreUrl);

        public void CheckUpdateFromButton() => CheckUpdateAsync(true);

        private async void CheckUpdateAsync(bool fromButton)
        {
            UpdateInfo info;
            try
            {
                info = await Task.Run(() => _updater.Fetch(Constants.CheckUrl));
            }
            catch
            {
                if (fromButton) MessageDialog.Show("检测更新失败", "更新提示");
                return;
            }

            if (string.IsNullOrEmpty(info.Version))
            {
                if (fromButton) MessageDialog.Show("检测更新失败", "更新提示");
                return;
            }

            if (UpdateService.IsNewer(info.Version, Constants.CurrentVersion))
            {
                var message = "检测到新版本，是否更新？" + Environment.NewLine + Environment.NewLine
                            + "以下是本次更新内容：" + Environment.NewLine + info.Info;
                if (info.IsForceUpdate)
                {
                    if (MessageDialog.Show(message, "更新提示", showCloseButton: false))
                    {
                        OpenUrl(info.DownloadUrl);
                        Application.Current.Shutdown();
                    }
                }
                else
                {
                    if (MessageDialog.YesNo(message, "更新提示"))
                    {
                        OpenUrl(info.DownloadUrl);
                    }
                }
            }
            else if (fromButton)
            {
                MessageDialog.Show("暂无更新", "更新提示");
            }
        }

        private static void OpenUrl(string url)
        {
            try
            {
                System.Diagnostics.Process.Start(url);
            }
            catch
            {
            }
        }
    }
}