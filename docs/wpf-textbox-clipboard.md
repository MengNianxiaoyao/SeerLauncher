# WPF 输入框剪贴板问题排查与修复参考

> 适用范围：.NET Framework / .NET（WPF）的桌面项目，含自定义窗口样式、无边框窗口。
> 来源：SeerLauncher 输入框“能粘贴不能复制、剪切无效”问题的完整排查结论，可整套搬到其他项目。

## 1. 症状速查表

| 症状 | 真因 | 对策 |
|---|---|---|
| 右键输入框，无复制菜单 | WPF `TextBox` 默认**没有**右键菜单（MS Learn 文档中相关描述有误，不要信） | §3.1：显式添加 `ContextMenu` |
| 剪切后文字还在、无任何提示 | 框架 `Cut` 是“先写剪贴板，失败就静默返回、不删字”（源码级实锤，见 §2.2） | §3.2：显式接管 `Cut`，失败弹提示 |
| 复制后关闭程序，再粘贴失效 | 老式写法（`SetDataObject` 不带 `copy:true`）不落盘 | 用 `SetText` / `SetDataObject(data, copy:true)`，自带落盘，**不要**再调 `Flush` |
| 复制/剪切明明成功却报“被占用” | 冗余的 `Flush`（或写入内部的 Flush 步骤）抛瞬时异常 | 上报失败前先读回校验，命中则视为成功 |
| 提权运行后 Win+V 历史里没有复制记录 | 系统 by-design：提权进程的复制不进历史记录/云同步 | 不用修（也修不了），见 §2.4 辨析 |

## 2. 关键结论（先看完再动手）

### 2.1 `TextBox` 默认没有右键菜单

不要被文档误导。没配 `ContextMenu` 的输入框上点右键是什么都不会发生的——用户以为的“右键复制失败”，其实是“复制根本没触发”；而 `Ctrl+V` 是直通的，于是呈现“能粘贴不能复制”。

### 2.2 框架的剪切/复制失败是**静默**的（核心坑）

`dotnet/wpf` 的 `TextEditorCopyPaste.cs` 中：

- `Cut(This, userInitiated)`：先 `_CreateDataObject`（会触发公开的 `Copying` 事件），再 `Clipboard.SetDataObject(data, copy: true)`；一旦抛 `ExternalException`（典型值 `CLIPBRD_E_CANT_OPEN`，即剪贴板正被剪贴板增强软件、远程桌面、词典划词等其他进程占用），`catch (ExternalException) when (!ShouldThrowOnCopyOrCutFailure)` **直接 `return`，选区不删除、无任何提示**（默认兼容开关下不抛）。
- `Copy` 同理，失败同样静默返回。

推论：**“剪切无效、字还在”只有两种可能——执行瞬间选区为空，或那一刻剪贴板被锁。** 两种都被框架吞掉，所以必须自己接管并给出反馈。

补充（实测修正）：写入是“先落盘、后 Flush”两步，`ExternalException` 可能发生在第二步——此时**数据实际已在剪贴板**，若直接报“被占用”就是误报（Win11 上剪贴板历史/查看器频繁开剪贴板，此类瞬时异常更多）。因此**上报失败前必须先读回校验**：`ContainsText() + GetText() == 预期` 命中则视为成功。另注意 `SetText` 等价于 `copy:true` 写入、自带落盘，**不要再调一次 `Clipboard.Flush()`**——多余的 Flush 只增加误报面，不增加可靠性。

补充（Win11 实测）：即使去掉了多余的 `Flush`，写入与读回校验仍可能撞上系统查看器的快照窗口而抛瞬时异常。对策是**退避重试吸收**（总预算约 1 秒：`50/100/200/400ms`），耗尽才认定失败。注意：远程同步类软件（向日葵/ToDesk/RDP 的剪贴板同步）会**长时间**持有剪贴板，250ms 级预算等不到它释放——QQ/Office 等成熟应用能正常复制，靠的也是更长的等待而非特殊通道；根治办法是暂停该软件的剪贴板同步。排查阶段可在失败提示里临时带上 `ex.ErrorCode`，按下表定位：

| 错误码 | 含义 |
|---|---|
| `0x800401D0` | `CLIPBRD_E_CANT_OPEN`：打不开剪贴板（被占中最常见，瞬时居多） |
| `0x800401D1` | `CLIPBRD_E_CANT_EMPTY`：清空失败 |
| `0x800401D2` | `CLIPBRD_E_CANT_SET`：写入失败 |
| `0x800401D3` | `CLIPBRD_E_CANT_CLOSE`：关闭失败 |

补充（持续 `0x800401D0` 的定位手段）：短重试也耗尽，说明有进程长时间拿着不放——用户态看不见的后台服务也算（远程/同步类、杀软内容检查等）。排查时可用 `user32.GetOpenClipboardWindow()` 查出占用者进程名（连服务消息窗口的占用者都能揪出来），写进报错：

### 2.3 `CanCut` 与 `CanCopy` 判定完全相同

`OnQueryStatusCut` / `OnQueryStatusCopy` 都是 `CanExecute = !Selection.IsEmpty`（可编辑、非 `PasswordBox` 前提下）。所以**“复制正常、剪切失败”不可能是命令路由层的问题**，不用往焦点、命令目标方向浪费时间，直接查选区状态和剪贴板锁。

### 2.4 提权的真实影响边界（纠正常见误判）

- 提权**不阻止**桌面程序之间的文本直接 `Ctrl+C / Ctrl+V`。如果程序开着、直接粘贴都没内容，提权不是原因。
- 提权真正受限的是：① 复制内容**不进 Win+V 历史记录和云同步**（by-design，防提权进程的秘密外泄）；② 低权限→高权限**拖放**被禁；③ 跨权限 `SendKeys` / 发消息被禁。
- 验证方法：任务管理器“详细信息”加“提升的”列确认；同一操作换非提权运行对比一次。

### 2.5 实例级 `CommandBinding` 可覆盖框架默认行为

给单个 `TextBox` 加 `CommandBinding`（`CanExecute` + `Executed` 均设 `e.Handled = true`）即可 shadow 掉类级的 `TextEditor` 内置处理，且**只影响这一个输入框**，`Ctrl+X` 快捷键和菜单项（同一命令）同时生效。复制/粘贴不加绑定则保持原样——按需逐个接管。

## 3. 通用修复模板（可直接抄）

### 3.1 XAML：菜单 + 命令绑定

```xml
<TextBox x:Name="InputBox">
    <TextBox.CommandBindings>
        <CommandBinding Command="ApplicationCommands.Cut"
                        CanExecute="OnEditCanExecute"
                        Executed="OnCutExecuted"/>
        <CommandBinding Command="ApplicationCommands.Copy"
                        CanExecute="OnEditCanExecute"
                        Executed="OnCopyExecuted"/>
        <CommandBinding Command="ApplicationCommands.Paste"
                        CanExecute="OnPasteCanExecute"
                        Executed="OnPasteExecuted"/>
    </TextBox.CommandBindings>
    <TextBox.ContextMenu>
        <ContextMenu>
            <MenuItem Header="剪切" Command="ApplicationCommands.Cut" InputGestureText="Ctrl+X"/>
            <MenuItem Header="复制" Command="ApplicationCommands.Copy" InputGestureText="Ctrl+C"/>
            <MenuItem Header="粘贴" Command="ApplicationCommands.Paste" InputGestureText="Ctrl+V"/>
        </ContextMenu>
    </TextBox.ContextMenu>
</TextBox>
```

### 3.2 C#：显式处理 + 明确反馈

```csharp
using System.Runtime.InteropServices; // ExternalException
using System.Windows;
using System.Windows.Input;

private bool IsEditable => InputBox != null && InputBox.IsEnabled && !InputBox.IsReadOnly;

private void OnEditCanExecute(object sender, CanExecuteRoutedEventArgs e)
{
    e.CanExecute = IsEditable && InputBox.SelectionLength > 0; // 选中为空时菜单自动置灰
    e.Handled = true;
}

private void OnPasteCanExecute(object sender, CanExecuteRoutedEventArgs e)
{
    e.CanExecute = IsEditable;
    e.Handled = true;
}

        private void OnCutExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            e.Handled = true;
            if (InputBox.SelectionLength == 0) return; // 未选中时菜单已置灰，静默返回即可
            if (!WriteClipboard(InputBox.SelectedText, "剪切")) return;
            InputBox.SelectedText = string.Empty;
        }

        private void OnCopyExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            e.Handled = true;
            if (InputBox.SelectionLength == 0) return;
            WriteClipboard(InputBox.SelectedText, "复制");
        }

private void OnPasteExecuted(object sender, ExecutedRoutedEventArgs e)
{
    e.Handled = true;
    string text;
    try
    {
        text = Clipboard.ContainsText() ? Clipboard.GetText() : null;
    }
    catch (ExternalException)
    {
        MessageBox.Show("剪贴板正被其他程序占用，粘贴失败，请稍后重试。", "粘贴");
        return;
    }
    if (string.IsNullOrEmpty(text)) return; // 空剪贴板静默返回
    InputBox.SelectedText = text; // 有选区则替换，无选区则在光标处插入
}

        private static bool WriteClipboard(string text, string caption)
{
    // SetText 自带落盘，不要再调 Flush；退避重试吸收长持有（远程同步类软件）
    int[] delays = { 50, 100, 200, 400 };
    foreach (int delay in delays)
    {
        try
        {
            Clipboard.SetText(text);
            return true;
        }
        catch (ExternalException)
        {
            if (IsClipboardText(text)) return true; // 落盘成功、后续步骤失败属于误报
            Thread.Sleep(delay);
        }
    }
    MessageBox.Show("剪贴板正被其他程序占用，请稍后重试。", caption);
    return false;
}

// 以下为排查期可选手段：耗尽后查占用者进程名，写进报错（需 using System.Diagnostics + System.Runtime.InteropServices）
private static string GetClipboardHolderName()
{
    try
    {
        var hwnd = GetOpenClipboardWindow();
        if (hwnd == IntPtr.Zero) return null;
        int pid;
        GetWindowThreadProcessId(hwnd, out pid);
        using (var process = Process.GetProcessById(pid))
            return process.ProcessName + ".exe";
    }
    catch
    {
        return null;
    }
}

[DllImport("user32.dll")]
private static extern IntPtr GetOpenClipboardWindow();

[DllImport("user32.dll")]
private static extern int GetWindowThreadProcessId(IntPtr hWnd, out int lpdwProcessId);

private static bool IsClipboardText(string expected)
{
    try
    {
        return Clipboard.ContainsText() && Clipboard.GetText() == expected;
    }
    catch (ExternalException)
    {
        return false;
    }
}
```

说明：

- 只捕获 `ExternalException`（剪贴板被占），与框架吞掉的异常口径一致；其他异常保持原样上抛，不掩盖真正的 bug。
- `SelectedText` 读写都是可撤销的常规编辑，不破坏 `Ctrl+Z`。
- 单行纯文本框用 `SetText`/`GetText` 足够；富文本框需另行处理格式（`DataFormats.Xaml` 等），不在本模板范围。

## 4. 验证清单

1. 选中文字 → 右键菜单剪切/复制/粘贴可用；未选中时剪切/复制置灰。
2. `Ctrl+X/C/V` 与菜单行为一致。
3. 复制 → 关闭程序 → 到记事本粘贴，内容仍在（`SetText` 自带落盘）。
4. 用剪贴板占用工具（或远程桌面会话）锁住剪贴板时操作，应看到明确的“被占用”提示而非无反应；Win11 开剪贴板历史时正常复制/剪切不应再误报。
5. `Ctrl+Z` 能撤销剪切/粘贴。

## 5. 附：本项目实例

- `SeerLauncher/Presentation/Windows/InputDialog.xaml`：菜单与绑定（程序内唯一的 `TextBox` 在此）。
- `SeerLauncher/Presentation/Windows/InputDialog.xaml.cs`：上述模板的落地实现（提示用的是项目自研 `MessageDialog`，其他项目照模板用 `MessageBox` 即可）。
