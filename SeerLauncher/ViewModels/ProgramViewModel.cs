using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Input;
using SeerLauncher.Mvvm;
using SeerLauncher.Services;

namespace SeerLauncher.ViewModels
{
    public class ProgramViewModel : ObservableObject
    {
        private readonly IConfigService _configService;
        private readonly IProgramScanService _scanner;
        private readonly IFileOperationsService _fileOps;
        private readonly IUiService _ui;
        private readonly string _runDirectory;
        private readonly string _selfName;
        private string _selectedItem;

        public ProgramViewModel(IConfigService configService, IProgramScanService scanner,
            IFileOperationsService fileOps, IUiService uiService, string runDirectory, string selfName)
        {
            _configService = configService ?? throw new ArgumentNullException(nameof(configService));
            _scanner = scanner ?? throw new ArgumentNullException(nameof(scanner));
            _fileOps = fileOps ?? throw new ArgumentNullException(nameof(fileOps));
            _ui = uiService ?? throw new ArgumentNullException(nameof(uiService));
            _runDirectory = runDirectory ?? throw new ArgumentNullException(nameof(runDirectory));
            _selfName = selfName ?? throw new ArgumentNullException(nameof(selfName));
            AddCommand = new RelayCommand(Add);
            LaunchCommand = new RelayCommand(Launch, () => SelectedItem != null);
            DeleteCommand = new RelayCommand(Delete, () => SelectedItem != null);
            OpenDirectoryCommand = new RelayCommand(OpenDirectory);
        }

        public ObservableCollection<string> Items { get; } = new ObservableCollection<string>();

        public string SelectedItem
        {
            get { return _selectedItem; }
            set { SetProperty(ref _selectedItem, value); }
        }

        public ICommand AddCommand { get; }
        public ICommand LaunchCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand OpenDirectoryCommand { get; }

        public void Refresh()
        {
            var config = _configService.Config;
            var configured = new List<string>(config.Programs.Keys);
            var scanned = _scanner.Scan(_runDirectory, _selfName, config.Keywords);
            Items.Clear();
            foreach (var item in _scanner.MergeConfiguredAndScanned(configured, scanned)) Items.Add(item);
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
                if (_ui.Confirm("是否添加《" + name + "》？")) AddFile(file);
            }
        }

        private void Add()
        {
            var fullPath = _ui.SelectExecutable();
            if (fullPath != null) AddFile(fullPath);
        }

        private void AddFile(string fullPath)
        {
            var name = Path.GetFileName(fullPath);
            if (!name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)) name += ".exe";
            _configService.Config.Programs[name] = Path.GetDirectoryName(fullPath) ?? "";
            _configService.Save();
            Refresh();
        }

        private void Launch()
        {
            var fullPath = GetSelectedPath();
            if (!_fileOps.Launch(fullPath))
                _ui.ShowMessage("启动失败，文件不存在：" + fullPath, "操作提示");
        }

        private void Delete()
        {
            var name = SelectedItem;
            if (!_ui.Confirm("是否将《" + name + "》移动到回收站？")) return;
            var fullPath = GetSelectedPath();
            if (!_fileOps.DeleteToRecycleBin(fullPath))
            {
                _ui.ShowMessage("删除失败，文件不存在或无法移动到回收站：" + fullPath, "操作提示");
                return;
            }
            _configService.Config.Programs.Remove(name);
            _configService.Save();
            Refresh();
        }

        private void OpenDirectory()
        {
            if (SelectedItem == null)
            {
                _ui.OpenDirectory(_runDirectory);
                return;
            }
            string path;
            _configService.Config.Programs.TryGetValue(SelectedItem, out path);
            _ui.OpenDirectory(string.IsNullOrEmpty(path) ? _runDirectory : path);
        }

        private string GetSelectedPath()
        {
            string path;
            _configService.Config.Programs.TryGetValue(SelectedItem, out path);
            return string.IsNullOrEmpty(path)
                ? Path.Combine(_runDirectory, SelectedItem)
                : Path.Combine(path, SelectedItem);
        }
    }
}
