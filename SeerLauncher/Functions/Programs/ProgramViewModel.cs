using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Input;
using SeerLauncher.Infrastructure.Configuration;
using SeerLauncher.Infrastructure.FileSystem;
using SeerLauncher.Infrastructure.Mvvm;
using SeerLauncher.Presentation.Services;
using SeerLauncher.Presentation.Windows;

namespace SeerLauncher.Functions.Programs
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
            var merged = _scanner.MergeConfiguredAndScanned(configured, scanned);

            // 内容没变就不重建集合，避免每次刷新都触发一整轮 UI 更新并丢失选中项
            if (IsSameAsItems(merged)) return;

            var selected = SelectedItem;
            Items.Clear();
            foreach (var item in merged) Items.Add(item);
            if (selected != null && Items.Contains(selected)) SelectedItem = selected;
        }

        private bool IsSameAsItems(List<string> merged)
        {
            if (merged.Count != Items.Count) return false;
            for (var index = 0; index < merged.Count; index++)
                if (!string.Equals(merged[index], Items[index], StringComparison.Ordinal)) return false;
            return true;
        }

        private void SaveConfig()
        {
            if (!_configService.Save())
                _ui.ShowMessage("配置保存失败，本次修改可能无法在下次启动时保留。", "保存失败");
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
            SaveConfig();
            Refresh();
        }

        private void Launch()
        {
            var name = SelectedItem;
            if (name == null) return;

            var result = _fileOps.Launch(GetSelectedPath());
            if (result == LaunchResult.Success) return;

            // 被拦截、未知错误：程序本身还在，只提示，保留记录
            if (result != LaunchResult.FileNotFound && result != LaunchResult.InvalidExecutable)
            {
                _ui.ShowMessage(BuildLaunchFailureMessage(result, name), "启动程序");
                return;
            }

            // 文件已不存在或不是有效程序：记录留着也无法再启动，清理后刷新
            _configService.Config.Programs.Remove(name);
            SaveConfig();
            _ui.ShowMessage(BuildLaunchFailureMessage(result, name), "启动程序");
            Refresh();
        }

        private static string BuildLaunchFailureMessage(LaunchResult result, string name)
        {
            switch (result)
            {
                case LaunchResult.FileNotFound:
                    return "《" + name + "》的文件已不存在，可能已被删除或移动到其他位置，该项已从列表中移除。";
                case LaunchResult.AccessDenied:
                    return "系统拒绝了《" + name + "》的启动请求。"
                        + Environment.NewLine + "该程序可能被安全软件拦截，或需要更高的权限才能运行。";
                case LaunchResult.InvalidExecutable:
                    return "《" + name + "》不是有效的可执行程序，文件可能已损坏或与当前系统的架构不匹配，该项已从列表中移除。";
                default:
                    return "启动《" + name + "》失败，请确认该程序可以正常运行。";
            }
        }

        private void Delete()
        {
            var name = SelectedItem;
            var choice = _ui.ShowDeleteChoice(
                "请选择《" + name + "》的删除方式：" + Environment.NewLine + "移到回收站可在需要时还原，彻底删除将无法恢复。",
                "删除程序");
            if (choice == DeleteChoice.Cancel) return;
            var fullPath = GetSelectedPath();
            var result = choice == DeleteChoice.Permanent
                ? _fileOps.DeletePermanently(fullPath)
                : _fileOps.DeleteToRecycleBin(fullPath);
            // 仍在运行/被占用、无权限、未知错误：记录保留，只提示
            if (result != DeleteResult.Success && result != DeleteResult.FileNotFound)
            {
                _ui.ShowMessage(BuildDeleteFailureMessage(result, name), "删除程序");
                return;
            }

            // 删除成功，或文件已不存在（记录留着也无法再删）：统一清理记录并刷新
            _configService.Config.Programs.Remove(name);
            SaveConfig();

            if (result != DeleteResult.Success)
                _ui.ShowMessage(BuildDeleteFailureMessage(result, name), "删除程序");

            Refresh();
        }

        private static string BuildDeleteFailureMessage(DeleteResult result, string name)
        {
            switch (result)
            {
                case DeleteResult.FileNotFound:
                    return "《" + name + "》的文件已不存在，可能已被删除或移动到其他位置，该项已从列表中移除。";
                case DeleteResult.FileInUse:
                    return "《" + name + "》正在运行或仍被其他程序占用，无法删除。"
                        + Environment.NewLine + "请先关闭该程序后重试。";
                case DeleteResult.AccessDenied:
                    return "没有删除《" + name + "》的权限，即使以管理员身份运行也无法删除。"
                        + Environment.NewLine + "该文件可能为只读、受系统保护，或其所在位置的权限被其他账户独占。";
                default:
                    return "删除《" + name + "》时发生未知错误，文件未能删除。";
            }
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
