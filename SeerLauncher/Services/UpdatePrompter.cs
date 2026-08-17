using System;

namespace SeerLauncher.Services
{
    public static class UpdatePrompter
    {
        public static bool PromptAndShouldExit(UpdateInfo info, bool notifyWhenNoUpdate)
        {
            if (string.IsNullOrEmpty(info.Version))
            {
                if (notifyWhenNoUpdate) MessageDialog.Show("检测更新失败", "更新提示");
                return false;
            }
            if (!UpdateService.IsNewer(info.Version, Constants.CurrentVersion))
            {
                if (notifyWhenNoUpdate) MessageDialog.Show("暂无更新", "更新提示");
                return false;
            }

            var message = "检测到新版本，是否更新？" + Environment.NewLine + Environment.NewLine
                        + "以下是本次更新内容：" + Environment.NewLine + info.Info;

            if (info.IsForceUpdate)
            {
                MessageDialog.Show(message, "更新提示", showCloseButton: false);
                OpenUrl(info.DownloadUrl);
                return true;
            }

            if (MessageDialog.YesNo(message, "更新提示"))
                OpenUrl(info.DownloadUrl);
            return false;
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