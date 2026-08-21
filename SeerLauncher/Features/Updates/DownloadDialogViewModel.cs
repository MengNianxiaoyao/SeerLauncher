using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using SeerLauncher.Infrastructure.Mvvm;
using SeerLauncher.Presentation.Services;

namespace SeerLauncher.Features.Updates
{
    public class DownloadDialogViewModel
    {
        public DownloadDialogViewModel(IEnumerable<DownloadLink> links, IUiService uiService)
        {
            if (links == null) throw new ArgumentNullException(nameof(links));
            if (uiService == null) throw new ArgumentNullException(nameof(uiService));
            Links = links.Select(link => new DownloadLinkViewModel(link, uiService)).ToList();
        }

        public IEnumerable<DownloadLinkViewModel> Links { get; }
    }

    public class DownloadLinkViewModel
    {
        private readonly IUiService _ui;

        public DownloadLinkViewModel(DownloadLink link, IUiService uiService)
        {
            if (link == null) throw new ArgumentNullException(nameof(link));
            _ui = uiService ?? throw new ArgumentNullException(nameof(uiService));
            Name = link.Name;
            Url = link.Url;
            OpenUrlCommand = new RelayCommand(OpenUrl, () => !string.IsNullOrEmpty(Url));
        }

        public string Name { get; }
        public string Url { get; }
        public ICommand OpenUrlCommand { get; }

        private void OpenUrl()
        {
            _ui.OpenUrl(Url);
        }
    }
}
