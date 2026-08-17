using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using SeerLauncher.Mvvm;
using SeerLauncher.Services;

namespace SeerLauncher.ViewModels
{
    public class KeywordViewModel : ObservableObject
    {
        private readonly IConfigService _configService;
        private readonly IUiService _ui;
        private string _selectedItem;

        public KeywordViewModel(IConfigService configService, IUiService uiService)
        {
            _configService = configService ?? throw new ArgumentNullException(nameof(configService));
            _ui = uiService ?? throw new ArgumentNullException(nameof(uiService));
            AddCommand = new RelayCommand(Add);
            ModifyCommand = new RelayCommand(Modify, () => SelectedItem != null);
            DeleteCommand = new RelayCommand(Delete, () => SelectedItem != null);
            Refresh();
        }

        public event EventHandler KeywordsChanged;

        public ObservableCollection<string> Items { get; } = new ObservableCollection<string>();

        public string SelectedItem
        {
            get { return _selectedItem; }
            set { SetProperty(ref _selectedItem, value); }
        }

        public ICommand AddCommand { get; }
        public ICommand ModifyCommand { get; }
        public ICommand DeleteCommand { get; }

        public void Refresh()
        {
            Items.Clear();
            foreach (var keyword in _configService.Config.Keywords) Items.Add(keyword);
        }

        private void Add()
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
            SaveAndNotify();
        }

        private void Modify()
        {
            var input = _ui.Prompt("请输入新的关键字", SelectedItem, "修改关键字");
            if (input == null) return;
            var keyword = input.Trim();
            if (!ConfigService.IsValidKeyword(keyword))
            {
                _ui.ShowMessage(string.IsNullOrEmpty(keyword)
                    ? "新的关键字不能为空"
                    : "关键字不能包含下列任何字符：" + Environment.NewLine + "\\/:*?\"" + "<>|", "操作提示");
                return;
            }
            var index = _configService.Config.Keywords.IndexOf(SelectedItem);
            if (_configService.Config.Keywords.Where((item, itemIndex) => itemIndex != index)
                .Any(item => string.Equals(item, keyword, StringComparison.OrdinalIgnoreCase)))
            {
                _ui.ShowMessage("该关键字已存在", "操作提示");
                return;
            }
            if (index >= 0) _configService.Config.Keywords[index] = keyword;
            SaveAndNotify();
        }

        private void Delete()
        {
            if (!_ui.Confirm("是否删除此关键字？")) return;
            _configService.Config.Keywords.Remove(SelectedItem);
            SaveAndNotify();
        }

        private void SaveAndNotify()
        {
            _configService.Save();
            Refresh();
            KeywordsChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
