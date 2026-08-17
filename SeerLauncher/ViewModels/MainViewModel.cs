using System;
using System.Windows.Input;
using SeerLauncher.Mvvm;
using SeerLauncher.Services;

namespace SeerLauncher.ViewModels
{
    public class MainViewModel
    {
        public MainViewModel(IConfigService configService, IProgramScanService scanner,
            IUpdateService updater, IFileOperationsService fileOps, IUiService uiService,
            string runDirectory, string selfName)
        {
            if (configService == null) throw new ArgumentNullException(nameof(configService));
            if (scanner == null) throw new ArgumentNullException(nameof(scanner));
            if (updater == null) throw new ArgumentNullException(nameof(updater));
            if (fileOps == null) throw new ArgumentNullException(nameof(fileOps));
            if (uiService == null) throw new ArgumentNullException(nameof(uiService));
            if (string.IsNullOrEmpty(runDirectory))
                throw new ArgumentException("Run directory is required.", nameof(runDirectory));
            if (string.IsNullOrEmpty(selfName))
                throw new ArgumentException("Executable name is required.", nameof(selfName));

            configService.Load();
            Keywords = new KeywordViewModel(configService, uiService);
            Programs = new ProgramViewModel(configService, scanner, fileOps, uiService, runDirectory, selfName);
            Updates = new UpdateViewModel(updater, uiService);
            RefreshDisplayCommand = new RelayCommand(RefreshDisplay);
            Keywords.KeywordsChanged += (sender, args) => Programs.Refresh();
            RefreshDisplay();

            if (configService.HasCorruptConfig)
            {
                uiService.RunOnLoaded(() =>
                    uiService.ShowMessage("配置文件已损坏，当前临时使用默认配置。原文件未被修改，保存新的配置时将覆盖该文件。", "配置警告"));
            }
        }

        public KeywordViewModel Keywords { get; }
        public ProgramViewModel Programs { get; }
        public UpdateViewModel Updates { get; }
        public ICommand RefreshDisplayCommand { get; }

        private void RefreshDisplay()
        {
            Keywords.Refresh();
            Programs.Refresh();
        }
    }
}
