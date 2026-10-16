---
name: wpf-desktop
description: C# WPF 无边框自定义窗口 + TextBox 剪贴板可靠化 + CI/发版流水线。自包含模板（全部代码内嵌，开箱即用）。当任务涉及新建 WPF 窗口、全局样式、无边框窗口、Win10/Win11 显示差异、右键菜单缺失、复制剪切无效、剪贴板占用卡 UI、加 CI/发版流水线时使用。
---

# WPF Desktop 通用 Skill（自包含）

> 本文件是唯一的依赖。不要引用任何外部仓库/文档做补充。
> 约定：下文 `YourApp` 一律替换为你的根命名空间；`YourApp.sln` 替换为你的解决方案名。
> 全部代码只用 C# 7.3 兼容语法，`net48`（含）以上任何 TFM 均可直接编译。

## 0. 移植步骤（5 步）

1. 新建 5 个文件：`Presentation/Controls/WindowEffects.cs`（§2.3）、`Presentation/Controls/CustomTitleBar.xaml(.cs)`（§2.2）、`Presentation/Windows/BaseWindow.cs`（§2.2）、`Presentation/Controls/TextBoxClipboard.cs`（§3.3）。
2. 合并 §2.1 的 `App.xaml` 资源（笔刷 + `AppWindowChrome` + 隐式样式）。
3. 每个窗口改继承 `BaseWindow`，`WindowStyle="None"`，加 `WindowChrome` 引用 + 首行 `CustomTitleBar`（§2.2 模板）。
4. 每个 `TextBox` 加 `controls:TextBoxClipboard.Enable="True"`（§3.2）。
5. 按需加 §4 的两个 workflow（把 `YourApp` 换成实际名）。

## 1. 项目结构约定

```text
YourApp/Presentation/Controls/   # 单文件控件：CustomTitleBar / TextBoxClipboard / WindowEffects
YourApp/Presentation/Windows/    # BaseWindow + 各窗口/对话框
YourApp/App.xaml                 # 唯一的全局样式入口
```

- 对话框/窗口继承 `BaseWindow`，不直接继承 `Window`。
- 新控件保持**单文件、无第三方依赖**。

## 2. 自定义窗口风格

### 2.1 App.xaml（完整可粘贴，隐式样式全项目自动生效）

```xml
<Application.Resources>
  <WindowChrome x:Key="AppWindowChrome" CaptionHeight="32" CornerRadius="0"
                GlassFrameThickness="1" ResizeBorderThickness="0"
                UseAeroCaptionButtons="False"/>
  <SolidColorBrush x:Key="WindowBgBrush" Color="#F4F6FB"/>
  <SolidColorBrush x:Key="CardBgBrush" Color="#FFFFFF"/>
  <SolidColorBrush x:Key="BorderBrush" Color="#E3E8F0"/>
  <SolidColorBrush x:Key="AccentBrush" Color="#4A6CF7"/>
  <SolidColorBrush x:Key="TextBrush" Color="#1F2937"/>
  <SolidColorBrush x:Key="MutedTextBrush" Color="#6B7280"/>

  <!-- 标题栏按钮（宽40高32；关闭键 hover 变红） -->
  <Style x:Key="CaptionButtonStyle" TargetType="Button">
    <Setter Property="FocusVisualStyle" Value="{x:Null}"/>
    <Setter Property="Width" Value="40"/>
    <Setter Property="Height" Value="32"/>
    <Setter Property="Background" Value="Transparent"/>
    <Setter Property="BorderThickness" Value="0"/>
    <Setter Property="Foreground" Value="#1F2937"/>
    <Setter Property="Cursor" Value="Hand"/>
    <Setter Property="Template">
      <Setter.Value>
        <ControlTemplate TargetType="Button">
          <Border x:Name="bd" Background="{TemplateBinding Background}">
            <ContentPresenter HorizontalAlignment="Center" VerticalAlignment="Center"/>
          </Border>
          <ControlTemplate.Triggers>
            <Trigger Property="IsMouseOver" Value="True">
              <Setter TargetName="bd" Property="Background" Value="#E5E9F2"/>
            </Trigger>
            <Trigger Property="IsPressed" Value="True">
              <Setter TargetName="bd" Property="Background" Value="#D5DAE6"/>
            </Trigger>
          </ControlTemplate.Triggers>
        </ControlTemplate>
      </Setter.Value>
    </Setter>
  </Style>
  <Style x:Key="CaptionCloseButtonStyle" TargetType="Button" BasedOn="{StaticResource CaptionButtonStyle}">
    <Setter Property="Template">
      <Setter.Value>
        <ControlTemplate TargetType="Button">
          <Border x:Name="bd" Background="{TemplateBinding Background}">
            <ContentPresenter HorizontalAlignment="Center" VerticalAlignment="Center"/>
          </Border>
          <ControlTemplate.Triggers>
            <Trigger Property="IsMouseOver" Value="True">
              <Setter TargetName="bd" Property="Background" Value="#E81123"/>
              <Setter Property="Foreground" Value="White"/>
            </Trigger>
            <Trigger Property="IsPressed" Value="True">
              <Setter TargetName="bd" Property="Background" Value="#B90E1E"/>
              <Setter Property="Foreground" Value="White"/>
            </Trigger>
          </ControlTemplate.Triggers>
        </ControlTemplate>
      </Setter.Value>
    </Setter>
  </Style>

  <!-- 输入框：Border(圆角6) > PART_ContentHost；触发器只改 BorderBrush -->
  <Style TargetType="TextBox">
    <Setter Property="Padding" Value="8,6"/>
    <Setter Property="Background" Value="#FFFFFF"/>
    <Setter Property="BorderBrush" Value="#E3E8F0"/>
    <Setter Property="BorderThickness" Value="1"/>
    <Setter Property="FontSize" Value="13"/>
    <Setter Property="Template">
      <Setter.Value>
        <ControlTemplate TargetType="TextBox">
          <Border x:Name="bd" Background="{TemplateBinding Background}"
                  BorderBrush="{TemplateBinding BorderBrush}"
                  BorderThickness="{TemplateBinding BorderThickness}"
                  CornerRadius="6" SnapsToDevicePixels="True">
            <ScrollViewer x:Name="PART_ContentHost" Focusable="False"
                          HorizontalScrollBarVisibility="Hidden"
                          VerticalScrollBarVisibility="Hidden"/>
          </Border>
          <ControlTemplate.Triggers>
            <Trigger Property="IsMouseOver" Value="True">
              <Setter TargetName="bd" Property="BorderBrush" Value="#4A6CF7"/>
            </Trigger>
            <Trigger Property="IsKeyboardFocused" Value="True">
              <Setter TargetName="bd" Property="BorderBrush" Value="#4A6CF7"/>
            </Trigger>
            <Trigger Property="IsEnabled" Value="False">
              <Setter TargetName="bd" Property="Opacity" Value="0.5"/>
            </Trigger>
          </ControlTemplate.Triggers>
        </ControlTemplate>
      </Setter.Value>
    </Setter>
  </Style>

  <!-- 右键菜单圆角卡片（代码生成的菜单自动套用，勿在业务代码另设样式） -->
  <Style TargetType="ContextMenu">
    <Setter Property="Background" Value="#FFFFFF"/>
    <Setter Property="BorderBrush" Value="#E3E8F0"/>
    <Setter Property="BorderThickness" Value="1"/>
    <Setter Property="Template">
      <Setter.Value>
        <ControlTemplate TargetType="ContextMenu">
          <Border Background="{TemplateBinding Background}"
                  BorderBrush="{TemplateBinding BorderBrush}"
                  BorderThickness="{TemplateBinding BorderThickness}"
                  CornerRadius="8" Padding="6" SnapsToDevicePixels="True">
            <ItemsPresenter/>
          </Border>
        </ControlTemplate>
      </Setter.Value>
    </Setter>
  </Style>
  <Style TargetType="MenuItem">
    <Setter Property="Background" Value="Transparent"/>
    <Setter Property="Foreground" Value="#1F2937"/>
    <Setter Property="FontSize" Value="13"/>
    <Setter Property="Padding" Value="10,6"/>
    <Setter Property="Template">
      <Setter.Value>
        <ControlTemplate TargetType="MenuItem">
          <Border x:Name="bd" Background="{TemplateBinding Background}"
                  Padding="{TemplateBinding Padding}" CornerRadius="5"
                  SnapsToDevicePixels="True">
            <ContentPresenter Content="{TemplateBinding Header}" VerticalAlignment="Center"/>
          </Border>
          <ControlTemplate.Triggers>
            <Trigger Property="IsMouseOver" Value="True">
              <Setter TargetName="bd" Property="Background" Value="#EEF1FE"/>
            </Trigger>
          </ControlTemplate.Triggers>
        </ControlTemplate>
      </Setter.Value>
    </Setter>
  </Style>
</Application.Resources>
```

其他控件（Button/ListBox/ScrollBar）样式按项目需要自行追加，模式相同。

### 2.2 窗口模板：BaseWindow + CustomTitleBar

每个窗口 XAML（`local` 指窗口所在命名空间，`controls` 指控件命名空间）：

```xml
<local:BaseWindow x:Class="YourApp.Presentation.Windows.YourWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:controls="clr-namespace:YourApp.Presentation.Controls"
        xmlns:local="clr-namespace:YourApp.Presentation.Windows"
        Title="..." Width="400" Height="200"
        WindowStartupLocation="CenterOwner"
        WindowStyle="None"
        Background="{StaticResource CardBgBrush}">
  <WindowChrome.WindowChrome>
    <StaticResource ResourceKey="AppWindowChrome"/>
  </WindowChrome.WindowChrome>
  <Grid>
    <Grid.RowDefinitions>
      <RowDefinition Height="Auto"/>
      <RowDefinition Height="*"/>
    </Grid.RowDefinitions>
    <controls:CustomTitleBar Grid.Row="0" Title="..."/>
    <!-- 内容放 Row 1 -->
  </Grid>
</local:BaseWindow>
```

`BaseWindow.cs`（完整）：

```csharp
using System;
using System.Windows;
using System.Windows.Input;
using YourApp.Presentation.Controls; // 改成你的命名空间

namespace YourApp.Presentation.Windows // 改成你的命名空间
{
    public abstract class BaseWindow : Window
    {
        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            WindowEffects.Apply(this);
        }

        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            if (e.Key == Key.Escape || e.Key == Key.Tab)
                e.Handled = true;

            if (e.Key == Key.System && (e.SystemKey == Key.LeftAlt || e.SystemKey == Key.RightAlt))
                e.Handled = true;
            base.OnPreviewKeyDown(e);
        }
    }
}
```

`CustomTitleBar.xaml`（完整）：

```xml
<UserControl x:Class="YourApp.Presentation.Controls.CustomTitleBar"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             Height="32">
  <UserControl.Resources>
    <BooleanToVisibilityConverter x:Key="BoolToVis"/>
  </UserControl.Resources>
  <Border Background="{StaticResource CardBgBrush}"
          BorderBrush="{StaticResource BorderBrush}" BorderThickness="0,0,0,1">
    <Grid>
      <Grid.ColumnDefinitions>
        <ColumnDefinition Width="*"/>
        <ColumnDefinition Width="Auto"/>
      </Grid.ColumnDefinitions>
      <StackPanel Grid.Column="0" Orientation="Horizontal" VerticalAlignment="Center" Margin="12,0,0,0">
        <Image Width="16" Height="16" Margin="0,0,8,0"
               Source="{Binding Icon, RelativeSource={RelativeSource AncestorType=UserControl}}">
          <Image.Style>
            <Style TargetType="Image">
              <Style.Triggers>
                <DataTrigger Binding="{Binding Icon, RelativeSource={RelativeSource AncestorType=UserControl}}" Value="{x:Null}">
                  <Setter Property="Visibility" Value="Collapsed"/>
                </DataTrigger>
              </Style.Triggers>
            </Style>
          </Image.Style>
        </Image>
        <TextBlock VerticalAlignment="Center" FontSize="14"
                   Foreground="{StaticResource TextBrush}"
                   Text="{Binding Title, RelativeSource={RelativeSource AncestorType=UserControl}}"/>
      </StackPanel>
      <StackPanel Grid.Column="1" Orientation="Horizontal">
        <!-- 注意：无边框下按钮必须加 WindowChrome.IsHitTestVisibleInChrome="True"，否则点不动 -->
        <Button Style="{StaticResource CaptionButtonStyle}" Content="─"
                WindowChrome.IsHitTestVisibleInChrome="True" Click="MinimizeBtn_Click"
                Visibility="{Binding ShowMinimizeButton, RelativeSource={RelativeSource AncestorType=UserControl}, Converter={StaticResource BoolToVis}}"/>
        <Button Style="{StaticResource CaptionCloseButtonStyle}" Content="✕"
                WindowChrome.IsHitTestVisibleInChrome="True" Click="CloseBtn_Click"
                Visibility="{Binding ShowCloseButton, RelativeSource={RelativeSource AncestorType=UserControl}, Converter={StaticResource BoolToVis}}"/>
      </StackPanel>
    </Grid>
  </Border>
</UserControl>
```

`CustomTitleBar.xaml.cs`（完整）：

```csharp
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace YourApp.Presentation.Controls // 改成你的命名空间
{
    public partial class CustomTitleBar : UserControl
    {
        public CustomTitleBar()
        {
            InitializeComponent();
        }

        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register(nameof(Title), typeof(string), typeof(CustomTitleBar), new PropertyMetadata(""));

        public string Title
        {
            get => (string)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        public static readonly DependencyProperty IconProperty =
            DependencyProperty.Register(nameof(Icon), typeof(ImageSource), typeof(CustomTitleBar), new PropertyMetadata(null));

        public ImageSource Icon
        {
            get => (ImageSource)GetValue(IconProperty);
            set => SetValue(IconProperty, value);
        }

        public static readonly DependencyProperty ShowMinimizeButtonProperty =
            DependencyProperty.Register(nameof(ShowMinimizeButton), typeof(bool), typeof(CustomTitleBar), new PropertyMetadata(false));

        public bool ShowMinimizeButton
        {
            get => (bool)GetValue(ShowMinimizeButtonProperty);
            set => SetValue(ShowMinimizeButtonProperty, value);
        }

        public static readonly DependencyProperty ShowCloseButtonProperty =
            DependencyProperty.Register(nameof(ShowCloseButton), typeof(bool), typeof(CustomTitleBar), new PropertyMetadata(true));

        public bool ShowCloseButton
        {
            get => (bool)GetValue(ShowCloseButtonProperty);
            set => SetValue(ShowCloseButtonProperty, value);
        }

        private void MinimizeBtn_Click(object sender, RoutedEventArgs e)
        {
            Window.GetWindow(this).WindowState = WindowState.Minimized;
        }

        private void CloseBtn_Click(object sender, RoutedEventArgs e)
        {
            Window.GetWindow(this)?.Close();
        }
    }
}
```

### 2.3 Win10 / Win11 自动适配：WindowEffects.cs（完整）

调用点固定在 `BaseWindow.OnSourceInitialized`（此时 HWND 已可用）。分两层：

1. **通用层（Win10 + Win11 都要）**：`WindowStyle=None` 会丢 `WS_THICKFRAME`，必须补回，DWM 才会画完整窗口阴影。无此一步，无边框窗口在两系统上都没阴影。
2. **Win11 专属层**：`build >= 22000`（读注册表 `CurrentBuildNumber`，结果缓存、只读一次）才调 `DwmSetWindowAttribute(DWMWA_WINDOW_CORNER_PREFERENCE=33, ROUND)`。该 attr 在 Win10 上不存在，必须**先分支再调用**，且调用包 `try/catch (TypeLoadException`，覆盖 `DllNotFound / EntryPointNotFound` 两种缺失`)`；P/Invoke 用返回 HRESULT 的签名，不许抛。注册表不可读时按 Win10 对待。

禁令：Win10 上不要调 attr 33，也不要在 WPF 内容里画窗口级圆角（会与 DWM 方形边框错位露角）；`WindowChrome.CornerRadius` 保持 `0`，圆角只交给 Win11 DWM；允许最大化的窗口需另行处理最大化态，此处不覆盖。

```csharp
using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using Microsoft.Win32;

namespace YourApp.Presentation.Controls // 改成你的命名空间
{
    internal static class WindowEffects
    {
        private const int DWMWA_WINDOW_CORNER_PREFERENCE = 33;
        private const int DWMWCP_ROUND = 2;
        private const int GWL_STYLE = -16;
        private const int WS_THICKFRAME = 0x00040000;

        // 检测结果缓存：注册表只读一次，避免每个窗口重复查
        private static readonly bool IsWindows11OrLater = DetectWindows11OrLater();

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hwnd, int index);

        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hwnd, int index, int value);

        public static void Apply(Window window)
        {
            var hwnd = new WindowInteropHelper(window).Handle;
            if (hwnd == IntPtr.Zero) return;

            var style = GetWindowLong(hwnd, GWL_STYLE);
            if ((style & WS_THICKFRAME) == 0)
                SetWindowLong(hwnd, GWL_STYLE, style | WS_THICKFRAME);

            if (IsWindows11OrLater)
            {
                try
                {
                    var corner = DWMWCP_ROUND;
                    DwmSetWindowAttribute(hwnd, DWMWA_WINDOW_CORNER_PREFERENCE, ref corner, sizeof(int));
                }
                catch (TypeLoadException)
                {
                    // dwmapi 缺失或无该入口（老系统/精简版 DWM）：忽略，只要阴影不要圆角
                }
            }
        }

        private static bool DetectWindows11OrLater()
        {
            try
            {
                var value = Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion",
                    "CurrentBuildNumber", null);
                return value != null && int.TryParse(value.ToString(), out var build) && build >= 22000;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
```

## 3. TextBox 剪贴板可靠化

### 3.1 症状速查（先对号入座）

| 症状 | 真因 | 对策 |
|---|---|---|
| 右键无复制菜单 | `TextBox` 默认**没有**右键菜单（官方文档相关描述有误，不要信） | §3.3 代码生成 ContextMenu |
| 剪切后字还在、无提示 | 框架 `Cut` 是"先写剪贴板，抛 `ExternalException` 就静默 return、不删字" | 显式接管 Cut，写成功才删字 |
| 复制时 UI 卡顿 | 重试 `Sleep` 跑在 UI 线程 | 写操作搬后台 STA 线程，UI 只 `await` |
| 剪贴板常被占用时无内容 | 托管 `Clipboard`（OLE + 内部 Flush）环节多、短重试等不到释放 | 原生 `OpenClipboard` 独占抢占 + 约 10 秒重试 |
| 提权运行后 Win+V 无记录 | by-design（防提权进程泄密），修不了 | 不用修；直接 `Ctrl+C / Ctrl+V` 不受影响 |

补充判定：`CanCut / CanCopy` 判定条件完全相同（`!Selection.IsEmpty`），故"复制行、剪切不行"不可能是命令路由问题，直接查选区与剪贴板锁。错误码 `0x800401D0=CANT_OPEN / D1=CANT_EMPTY / D2=CANT_SET / D3=CANT_CLOSE`；持续 `D0` 说明有进程长时间占用，可用 `user32.GetOpenClipboardWindow()` 揪出占用者。排查期可在失败提示里暂带 `ex.ErrorCode`。

### 3.2 接入：XAML 一行 + 按需直接调用

```xml
<!-- xmlns:controls="clr-namespace:YourApp.Presentation.Controls" -->
<TextBox controls:TextBoxClipboard.Enable="True"/>
```

`Enable="True"` 自动完成：挂剪切/复制/粘贴命令接管 + 生成右键菜单（`CommandTarget` 显式指向本输入框，不依赖焦点）；置 `False` 完整卸载。不经过输入框的复制（如按钮一键复制）直接调：

```csharp
bool copied = await TextBoxClipboard.TrySetTextAsync(text);
```

终版三招（实现原理，不要偏离）：① 原生单步抢占 `OpenClipboard → EmptyClipboard → SetClipboardData(CF_UNICODETEXT) → CloseClipboard`，成功即权威成功，不做托管读回、不调 `Flush`（原生自带落盘，关程序仍可粘贴）；② 后台 STA 线程每 10ms 抢一次、最多约 1000 次（≈10秒），UI 只 `await`；③ 剪切先存选区，写成功后复核"仍可编辑且选区未变"才删字。纯文本只用 `CF_UNICODETEXT`；富文本另行处理。`SelectedText` 读写是可撤销的常规编辑，不破坏 `Ctrl+Z`；粘贴后光标钉到插入末尾。

### 3.3 TextBoxClipboard.cs（完整，改命名空间即用）

失败提示用系统 `MessageBox`（如项目有自研对话框，替换 `ShowClipboardBusy` 一处即可）：

```csharp
using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace YourApp.Presentation.Controls // 改成你的命名空间
{
    public static class TextBoxClipboard
    {
        public static readonly DependencyProperty EnableProperty =
            DependencyProperty.RegisterAttached(
                "Enable",
                typeof(bool),
                typeof(TextBoxClipboard),
                new PropertyMetadata(false, OnEnableChanged));

        public static bool GetEnable(DependencyObject target)
        {
            return (bool)target.GetValue(EnableProperty);
        }

        public static void SetEnable(DependencyObject target, bool value)
        {
            target.SetValue(EnableProperty, value);
        }

        private static void OnEnableChanged(DependencyObject target, DependencyPropertyChangedEventArgs e)
        {
            var textBox = target as TextBox;
            if (textBox == null)
            {
                return;
            }

            if ((bool)e.NewValue)
            {
                Attach(textBox);
            }
            else
            {
                Detach(textBox);
            }
        }

        private static void Attach(TextBox textBox)
        {
            // 实例级 CommandBinding shadow 掉框架默认行为，只影响这一个输入框
            textBox.CommandBindings.Add(new CommandBinding(ApplicationCommands.Cut, OnCutExecuted, OnEditCanExecute));
            textBox.CommandBindings.Add(new CommandBinding(ApplicationCommands.Copy, OnCopyExecuted, OnEditCanExecute));
            textBox.CommandBindings.Add(new CommandBinding(ApplicationCommands.Paste, OnPasteExecuted, OnPasteCanExecute));
            EnsureContextMenu(textBox);
        }

        private static void Detach(TextBox textBox)
        {
            for (var index = textBox.CommandBindings.Count - 1; index >= 0; index--)
            {
                var binding = textBox.CommandBindings[index];
                if (binding.Command == ApplicationCommands.Cut ||
                    binding.Command == ApplicationCommands.Copy ||
                    binding.Command == ApplicationCommands.Paste)
                {
                    textBox.CommandBindings.RemoveAt(index);
                }
            }

            textBox.ClearValue(FrameworkElement.ContextMenuProperty);
        }

        private static void EnsureContextMenu(TextBox textBox)
        {
            if (textBox.ContextMenu != null)
            {
                return;
            }

            // 样式走 App.xaml 隐式样式，此处不再另设
            var menu = new ContextMenu();
            menu.Items.Add(CreateMenuItem("剪切", ApplicationCommands.Cut, "Ctrl+X", textBox));
            menu.Items.Add(CreateMenuItem("复制", ApplicationCommands.Copy, "Ctrl+C", textBox));
            menu.Items.Add(CreateMenuItem("粘贴", ApplicationCommands.Paste, "Ctrl+V", textBox));
            textBox.ContextMenu = menu;
        }

        private static MenuItem CreateMenuItem(string header, RoutedUICommand command, string gesture, TextBox target)
        {
            var item = new MenuItem();
            item.Header = header;
            item.Command = command;
            item.CommandTarget = target;
            item.InputGestureText = gesture;
            return item;
        }

        private static bool IsEditable(TextBox textBox)
        {
            return textBox != null && textBox.IsEnabled && !textBox.IsReadOnly;
        }

        private static void OnEditCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            var textBox = sender as TextBox;
            e.CanExecute = IsEditable(textBox) && textBox.SelectionLength > 0;
            e.Handled = true;
        }

        private static void OnPasteCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = IsEditable(sender as TextBox);
            e.Handled = true;
        }

        private static async void OnCutExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            e.Handled = true;
            var textBox = sender as TextBox;
            if (!IsEditable(textBox))
            {
                return;
            }

            if (textBox.SelectionLength == 0)
            {
                return;
            }

            var selectedText = textBox.SelectedText;
            bool written;
            try
            {
                written = await TrySetTextAsync(selectedText);
            }
            catch (InvalidOperationException)
            {
                written = false;
            }

            if (!written)
            {
                ShowClipboardBusy("剪切");
                return;
            }

            // 异步等待期间用户可能改了选区，复核一致才删字，避免误删
            if (!IsEditable(textBox) || textBox.SelectedText != selectedText)
            {
                return;
            }

            textBox.SelectedText = string.Empty;
        }

        private static async void OnCopyExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            e.Handled = true;
            var textBox = sender as TextBox;
            if (!IsEditable(textBox))
            {
                return;
            }

            if (textBox.SelectionLength == 0)
            {
                return;
            }

            var selectedText = textBox.SelectedText;
            bool written;
            try
            {
                written = await TrySetTextAsync(selectedText);
            }
            catch (InvalidOperationException)
            {
                written = false;
            }

            if (!written)
            {
                ShowClipboardBusy("复制");
            }
        }

        private static void OnPasteExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            e.Handled = true;
            var textBox = sender as TextBox;
            if (!IsEditable(textBox))
            {
                return;
            }

            string text;
            try
            {
                text = Clipboard.ContainsText() ? Clipboard.GetText() : null;
            }
            catch (ExternalException)
            {
                ShowClipboardBusy("粘贴");
                return;
            }

            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            var insertionStart = textBox.SelectionStart;
            textBox.SelectedText = text;
            textBox.Select(insertionStart + text.Length, 0); // 光标钉到末尾，消掉选区
        }

        public static bool TrySetText(string text)
        {
            for (var attempt = 0; attempt < 1000; attempt++)
            {
                if (TryOpenAndSet(text))
                {
                    return true;
                }

                Thread.Sleep(10); // 跑在后台 STA 线程，不卡 UI
            }

            return false;
        }

        private static bool TryOpenAndSet(string text)
        {
            if (!OpenClipboard(IntPtr.Zero))
            {
                return false;
            }

            try
            {
                if (!EmptyClipboard())
                {
                    return false;
                }

                var handle = GlobalAlloc(GmemMoveable | GmemZeroInit, (UIntPtr)(uint)((text.Length + 1) * 2));
                if (handle == IntPtr.Zero)
                {
                    return false;
                }

                var pointer = GlobalLock(handle);
                if (pointer == IntPtr.Zero)
                {
                    GlobalFree(handle);
                    return false;
                }

                try
                {
                    Marshal.Copy(text.ToCharArray(), 0, pointer, text.Length);
                }
                finally
                {
                    GlobalUnlock(handle);
                }

                if (SetClipboardData(CfUnicodeText, handle) == IntPtr.Zero)
                {
                    GlobalFree(handle);
                    return false;
                }

                // 成功后 ownership 移交系统，不可 Free
                return true;
            }
            finally
            {
                CloseClipboard();
            }
        }

        private const uint CfUnicodeText = 13;
        private const uint GmemMoveable = 0x0002;
        private const uint GmemZeroInit = 0x0040;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool OpenClipboard(IntPtr hWndNewOwner);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool CloseClipboard();

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool EmptyClipboard();

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SetClipboardData(uint uFormat, IntPtr hMem);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GlobalAlloc(uint uFlags, UIntPtr dwBytes);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GlobalLock(IntPtr hMem);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool GlobalUnlock(IntPtr hMem);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GlobalFree(IntPtr hMem);

        public static Task<bool> TrySetTextAsync(string text)
        {
            var completion = new TaskCompletionSource<bool>();
            var thread = new Thread(() =>
            {
                try
                {
                    completion.SetResult(TrySetText(text));
                }
                catch (Exception exception)
                {
                    completion.SetException(exception);
                }
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.IsBackground = true;
            thread.Start();
            return completion.Task;
        }

        private static void ShowClipboardBusy(string caption)
        {
            // 有自研对话框的项目替换这一处即可
            MessageBox.Show("剪贴板正被其他程序占用，请稍后重试。", caption,
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }
}
```

## 4. 自动化构建与发布（CI + Release 双流水线）

两文件都在 `.github/workflows/`，都只在 `push: branches: [main]` 触发，用 head commit message 前缀分流，互斥执行：

- `chore: v*` → 走 Release；其他 → 走 CI。
  - CI：`if: "!startsWith(github.event.head_commit.message, 'chore: v')"`
  - Release：`if: "startsWith(github.event.head_commit.message, 'chore: v')"` + `permissions: contents: write`
- `YourApp` 换成实际项目名；`net48` 换成实际 TFM（两处：`upload-artifact.path` 与 `Compress-Archive -Path` 必须同步）。

`.github/workflows/ci.yml`：

```yaml
name: CI

on:
  push:
    branches: [main]

jobs:
  build:
    if: "!startsWith(github.event.head_commit.message, 'chore: v')"
    runs-on: windows-latest
    steps:
      - uses: actions/checkout@v7

      - uses: microsoft/setup-msbuild@v3

      - name: Build
        run: msbuild YourApp.sln /restore /p:Configuration=Release

      - name: Upload artifacts
        uses: actions/upload-artifact@v7
        with:
          name: YourApp
          path: YourApp/bin/Release/net48/*.exe
```

要点：必须 `windows-latest`（Framework WPF 离不开 MSBuild/Windows）；`/restore` 不可省。

`.github/workflows/release.yml`：

```yaml
name: Release

on:
  push:
    branches: [main]

permissions:
  contents: write

jobs:
  release:
    if: "startsWith(github.event.head_commit.message, 'chore: v')"
    runs-on: windows-latest
    steps:
      - uses: actions/checkout@v7

      - uses: microsoft/setup-msbuild@v3

      - name: Extract version
        id: version
        shell: pwsh
        run: |
          $msg = "${{ github.event.head_commit.message }}"
          $ver = $msg -replace '^chore: v(.+)$', '$1'
          echo "version=$ver" >> $env:GITHUB_OUTPUT

      - name: Build
        run: msbuild YourApp.sln /restore /p:Configuration=Release

      - name: Package
        shell: pwsh
        run: Compress-Archive -Path YourApp/bin/Release/net48/*.exe -DestinationPath YourApp.zip

      - name: Create Release
        uses: softprops/action-gh-release@v3
        with:
          tag_name: v${{ steps.version.outputs.version }}
          name: v${{ steps.version.outputs.version }}
          body: ${{ github.event.head_commit.message }}
          files: YourApp.zip
        env:
          GITHUB_TOKEN: ${{ secrets.GITHUB_TOKEN }}
```

发版操作：提交 `chore: v<版本号>` 并 push 到 main 即可触发，`tag_name` 与版本号同源。若输出含 dll/config/资源，把 `Compress-Archive -Path` 扩大到整个输出目录，否则发出去的包跑不起来。

## 5. 验证清单（交工前逐条过）

1. 选中文字→右键剪切/复制/粘贴可用；未选中时剪切/复制置灰。
2. `Ctrl+X/C/V` 与菜单行为一致。
3. 复制→关闭程序→记事本粘贴，内容仍在（原生写入自带落盘）。
4. 剪贴板被占用时复制/剪切：成功（最多顿一下）或明确"被占用"提示；UI 不冻结。
5. `Ctrl+Z` 能撤销剪切/粘贴。
6. 新窗口：无边框 + 自绘标题栏、最小化/关闭可用；Win10 方形直角+阴影、Win11 圆角+阴影；Esc/Tab/Alt 不会误关。
7. CI：普通 push 到 main 能构建出 artifact；`chore: vX.Y.Z` push 能生成对应 tag 的 Release 并附带 zip。
