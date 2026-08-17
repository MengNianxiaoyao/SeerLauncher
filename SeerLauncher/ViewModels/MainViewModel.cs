using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using SeerLauncher.Mvvm;
using SeerLauncher.Models;
using SeerLauncher.Services;

namespace SeerLauncher.ViewModels
{
    public class MainViewModel : ObservableObject
    {
        private readonly IConfigService _configService;
        private readonly IProgramScanService _scanner;
        private readonly IUpdateService _updater;
        private readonly IFileOperationsService _fileOps;
        private readonly IUiService _ui;
        private readonly string _runDirectory;
        private readonly string _selfName;

        public MainViewModel(IConfigService configService, IProgramScanService scanner,
            IUpdateService updater, IFileOperationsService fileOps, IUiService uiService,
            string runDirectory, string selfName)
        {
            _configService = configService ?? throw new ArgumentNullException(nameof(configService));
            _scanner = scanner ?? throw new ArgumentNullException(nameof(scanner));
            _updater = updater ?? throw new ArgumentNullException(nameof(updater));
            _fileOps = fileOps ?? throw new ArgumentNullException(nameof(fileOps));
            _ui = uiService ?? throw new ArgumentNullException(nameof(uiService));
            _runDirectory = string.IsNullOrEmpty(runDirectory)
                ? throw new ArgumentException("Run directory is required.", nameof(runDirectory))
                : runDirectory;
            _selfName = string.IsNullOrEmpty(selfName)
                ? throw new ArgumentException("Executable name is required.", nameof(selfName))
                : selfName;

            var config = _configService.Load();
            foreach (var keyword in config.Keywords) Keywords.Add(keyword);
            RefreshDisplay();

            if (_configService.HasCorruptConfig)
            {
                _ui.RunOnLoaded(() =>
                    _ui.ShowMessage("配置文件已损坏，当前临时使用默认配置。原文件未被修改，保存新的配置时将覆盖该文件。", "配置警告"));
            }

            AddKeywordCommand = new RelayCommand(AddKeyword);
            ModifyKeywordCommand = new RelayCommand(ModifyKeyword, () => SelectedKeyword != null);
            DeleteKeywordCommand = new RelayCommand(DeleteKeyword, () => SelectedKeyword != null);
            AddProgramCommand = new RelayCommand(AddProgram);
            LaunchProgramCommand = new RelayCommand(LaunchSelected, () => SelectedProgram != null);
            DeleteProgramCommand = new RelayCommand(DeleteProgram, () => SelectedProgram != null);
            OpenDirectoryCommand = new RelayCommand(OpenDirectory);
            RefreshDisplayCommand = new RelayCommand(RefreshDisplay);
            AuxiliaryDownloadCommand = new AsyncRelayCommand(AuxiliaryDownloadAsync,
                onException: _ => _ui.ShowMessage("获取下载链接失败", "操作提示"));
            CheckUpdateCommand = new AsyncRelayCommand(() => CheckUpdateAsync(true),
                onException: _ => _ui.ShowMessage("检测更新失败", "更新提示"));
            OpenInstructionsCommand = new RelayCommand(OpenInstructions);
            OpenBilibiliCommand = new RelayCommand(OpenBilibili);
            OpenStoreCommand = new RelayCommand(OpenStore);

            _ui.RunInBackground(() => CheckUpdateAsync(false));
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
        public ICommand AuxiliaryDownloadCommand { get; }
        public ICommand CheckUpdateCommand { get; }
        public ICommand OpenInstructionsCommand { get; }
        public ICommand OpenBilibiliCommand { get; }
        public ICommand OpenStoreCommand { get; }

        private void RefreshDisplay()
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

        private void AddKeyword()
        {
            var input = _ui.Prompt("请输入要添加的关键字", "", "添加关键字");
            if (input == null) return;
            var keyword = input.Trim();
            if (!ConfigService.IsValidKeyword(keyword))
            {
                _ui.ShowMessage(string.IsNullOrEmpty(keyword)
                    ? "添加的关键字不能为空"
                    : "关键字不能包含下列任何字符：" + Environment.NewLine + "\\/:*?\"" + "<>|", "操作提示");
                return;
            }
            if (_configService.Config.Keywords.Any(item => string.Equals(item, keyword, StringComparison.OrdinalIgnoreCase)))
            {
                _ui.ShowMessage("该关键字已存在", "操作提示");
                return;
            }
            _configService.Config.Keywords.Add(keyword);
            _configService.Save();
            RefreshDisplay();
        }

        private void ModifyKeyword()
        {
            var input = _ui.Prompt("请输入新的关键字", SelectedKeyword, "修改关键字");
            if (input == null) return;
            var keyword = input.Trim();
            if (!ConfigService.IsValidKeyword(keyword))
            {
                _ui.ShowMessage(string.IsNullOrEmpty(keyword)
                    ? "新的关键字不能为空"
                    : "关键字不能包含下列任何字符：" + Environment.NewLine + "\\/:*?\"" + "<>|", "操作提示");
                return;
            }
            var index = _configService.Config.Keywords.IndexOf(SelectedKeyword);
            if (_configService.Config.Keywords.Where((item, itemIndex) => itemIndex != index)
                .Any(item => string.Equals(item, keyword, StringComparison.OrdinalIgnoreCase)))
            {
                _ui.ShowMessage("该关键字已存在", "操作提示");
                return;
            }
            if (index >= 0) _configService.Config.Keywords[index] = keyword;
            _configService.Save();
            RefreshDisplay();
        }

        private void DeleteKeyword()
        {
            if (!_ui.Confirm("是否删除此关键字？")) return;

            _configService.Config.Keywords.Remove(SelectedKeyword);
            _configService.Save();
            RefreshDisplay();
        }

        private void AddProgram()
        {
            var fullPath = _ui.SelectExecutable();
            if (fullPath != null) AddProgramFile(fullPath);
        }

        private void AddProgramFile(string fullPath)
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
                    _ui.ShowMessage("添加的文件不是可执行文件", "操作提示");
                    continue;
                }
                if (_ui.Confirm("是否添加《" + name + "》？"))
                {
                    AddProgramFile(file);
                }
            }
        }

        private void LaunchSelected()
        {
            var name = SelectedProgram;
            string path;
            _configService.Config.Programs.TryGetValue(name, out path);
            var fullPath = string.IsNullOrEmpty(path)
                ? Path.Combine(_runDirectory, name)
                : Path.Combine(path, name);
            if (!_fileOps.Launch(fullPath))
                _ui.ShowMessage("启动失败，文件不存在：" + fullPath, "操作提示");
        }

        private void DeleteProgram()
        {
            var name = SelectedProgram;
            if (!_ui.Confirm("是否将《" + name + "》移动到回收站？")) return;

            string path;
            _configService.Config.Programs.TryGetValue(name, out path);
            var fullPath = string.IsNullOrEmpty(path)
                ? Path.Combine(_runDirectory, name)
                : Path.Combine(path, name);

            if (!_fileOps.DeleteToRecycleBin(fullPath))
            {
                _ui.ShowMessage("删除失败，文件不存在或无法移动到回收站：" + fullPath, "操作提示");
                return;
            }
            _configService.Config.Programs.Remove(name);
            _configService.Save();
            RefreshDisplay();
        }

        private void OpenDirectory()
        {
            if (SelectedProgram == null)
            {
                _ui.OpenDirectory(_runDirectory);
                return;
            }
            var name = SelectedProgram;
            string path;
            _configService.Config.Programs.TryGetValue(name, out path);
            _ui.OpenDirectory(string.IsNullOrEmpty(path) ? _runDirectory : path);
        }

        private async Task AuxiliaryDownloadAsync()
        {
            List<DownloadLink> links;
            try
            {
                links = await Task.Run(() => _updater.FetchLinks(Constants.CheckUrl));
            }
            catch
            {
                _ui.ShowMessage("获取下载链接失败", "操作提示");
                return;
            }
            if (links.Count == 0)
            {
                _ui.ShowMessage("获取下载链接失败", "操作提示");
                return;
            }
            _ui.ShowDownloadLinks(links);
        }

        private void OpenInstructions() => _ui.OpenUrl(Constants.InstructionsUrl);
        private void OpenBilibili() => _ui.OpenUrl(Constants.DeveloperBilibili);
        private void OpenStore() => _ui.OpenUrl(Constants.StoreUrl);

        private async Task CheckUpdateAsync(bool fromButton)
        {
            UpdateInfo info;
            try
            {
                info = await Task.Run(() => _updater.Fetch(Constants.CheckUrl));
            }
            catch
            {
                if (fromButton) _ui.ShowMessage("检测更新失败", "更新提示");
                return;
            }

            if (string.IsNullOrEmpty(info.Version))
            {
                if (fromButton) _ui.ShowMessage("检测更新失败", "更新提示");
                return;
            }

            if (UpdateService.IsNewer(info.Version, Constants.CurrentVersion))
            {
                var message = "检测到新版本，是否更新？" + Environment.NewLine + Environment.NewLine
                            + "以下是本次更新内容：" + Environment.NewLine + info.Info;
                var choice = _ui.ShowUpdate(message, "更新提示", showCloseButton: !info.IsForceUpdate);
                if (choice == UpdateChoice.Cancel)
                {
                    if (info.IsForceUpdate) _ui.Shutdown();
                    return;
                }
                var url = choice == UpdateChoice.Global ? info.GlobalUrl : info.CnUrl;
                if (!string.IsNullOrEmpty(url)) _ui.OpenUrl(url);
                if (info.IsForceUpdate) _ui.Shutdown();
            }
            else if (fromButton)
            {
                _ui.ShowMessage("暂无更新", "更新提示");
            }
        }

    }
}
