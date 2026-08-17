using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Input;
using SeerLauncher.Models;
using SeerLauncher.Mvvm;
using SeerLauncher.Services;

namespace SeerLauncher.ViewModels
{
    public class UpdateViewModel
    {
        private readonly IUpdateService _updater;
        private readonly IUiService _ui;

        public UpdateViewModel(IUpdateService updater, IUiService uiService)
        {
            _updater = updater ?? throw new ArgumentNullException(nameof(updater));
            _ui = uiService ?? throw new ArgumentNullException(nameof(uiService));
            AuxiliaryDownloadCommand = new AsyncRelayCommand(AuxiliaryDownloadAsync,
                onException: _ => _ui.ShowMessage("获取下载链接失败", "操作提示"));
            CheckUpdateCommand = new AsyncRelayCommand(() => CheckUpdateAsync(true),
                onException: _ => _ui.ShowMessage("检测更新失败", "更新提示"));
            OpenInstructionsCommand = new RelayCommand(() => _ui.OpenUrl(Constants.InstructionsUrl));
            OpenBilibiliCommand = new RelayCommand(() => _ui.OpenUrl(Constants.DeveloperBilibili));
            OpenStoreCommand = new RelayCommand(() => _ui.OpenUrl(Constants.StoreUrl));
            _ui.RunInBackground(() => CheckUpdateAsync(false));
        }

        public string VersionText => "Ver: " + Constants.CurrentVersion;
        public ICommand AuxiliaryDownloadCommand { get; }
        public ICommand CheckUpdateCommand { get; }
        public ICommand OpenInstructionsCommand { get; }
        public ICommand OpenBilibiliCommand { get; }
        public ICommand OpenStoreCommand { get; }

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
                var choice = _ui.ShowUpdate(message, "更新提示", !info.IsForceUpdate);
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
