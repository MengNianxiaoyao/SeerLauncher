# WPF 输入框剪贴板问题排查与修复参考

> 适用范围：.NET Framework / .NET（WPF）的桌面项目，含自定义窗口样式、无边框窗口。
> 来源：SeerLauncher 输入框“能粘贴不能复制、剪切无效”问题的完整排查结论，可整套搬到其他项目。

## 1. 症状速查表

| 症状 | 真因 | 对策 |
|---|---|---|
| 右键输入框，无复制菜单 | WPF `TextBox` 默认**没有**右键菜单（MS Learn 文档中相关描述有误，不要信） | §3.1：一行附加属性接入，菜单自动生成 |
| 剪切后文字还在、无任何提示 | 框架 `Cut` 是“先写剪贴板，失败就静默返回、不删字”（源码级实锤，见 §2.2） | §3：显式接管 `Cut`，写成功才删字 |
| 复制/剪切时 UI 卡顿 | 重试 `Thread.Sleep` 跑在 UI 线程 | 写操作搬到后台 STA 线程，UI 只 `await`（见 §2.6） |
| 远程同步常驻时剪贴板无内容/频繁报错 | 托管写入（OLE + 内部 Flush）环节多、重试预算短，等不到释放 | 原生 `OpenClipboard` 独占抢占 + 约 10 秒重试，`SetClipboardData` 成功即权威成功（见 §2.6） |
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

### 2.6 终版方案：原生写入 + 后台线程（替代托管 `Clipboard` 类）

托管 `Clipboard.SetText` 走 OLE（`OleSetClipboard` + 内部 Flush），环节多、误报多（见 §2.2 补充），且重试只能跑在 UI 线程（卡 UI）。终版改用三招：

1. **原生单步抢占**：`OpenClipboard → EmptyClipboard → SetClipboardData(CF_UNICODETEXT) → CloseClipboard`。抢到独占即拥有，`SetClipboardData` 成功即**权威成功**——不做托管读回、不调 `Flush`（原生写入自带落盘，关闭程序后仍可粘贴）。
2. **后台 STA 线程 + 长重试**：写操作在专用后台线程（`SetApartmentState(STA)`）每 10ms 抢一次、最多约 10 秒；UI 线程只 `await`，从不 `Sleep`，不卡界面。远程同步类软件的长持有也能等过去。
3. **剪切先存后删**：`await` 前记下 `SelectedText`，写成功后复核“仍可编辑且选区未变”才删字，避免异步等待期间用户改了选区导致误删。

## 3. 通用修复模板：`TextBoxClipboard` 附加行为（可直接抄）

完整实现见本仓库 `SeerLauncher/Presentation/Controls/TextBoxClipboard.cs`（单文件、无第三方依赖），下面是用法与核心模式。

### 3.1 接入：XAML 一行 + 按需直接调用

```xml
<!-- xmlns:controls="clr-namespace:SeerLauncher.Presentation.Controls" -->
<TextBox x:Name="InputBox"
         controls:TextBoxClipboard.Enable="True"/>
```

`Enable="True"` 自动完成三件事：挂剪切/复制/粘贴的命令接管、生成右键菜单（命令显式指定 `CommandTarget`，不依赖焦点）。置 `False` 可完整卸载。不经过输入框的复制（如按钮一键复制）直接调：

```csharp
bool copied = await TextBoxClipboard.TrySetTextAsync(text);
```

### 3.2 核心模式（完整代码见 `TextBoxClipboard.cs`，不再全文复述）

```csharp
// 原生单步抢占：抢到独占即拥有；后台线程每 10ms 抢一次，最多约 10 秒
public static bool TrySetText(string text)
{
    for (var attempt = 0; attempt < 1000; attempt++)
    {
        if (TryOpenAndSet(text)) return true;
        Thread.Sleep(10); // 跑在后台 STA 线程，不卡 UI
    }
    return false;
}

// OpenClipboard → EmptyClipboard → SetClipboardData(CF_UNICODETEXT) → CloseClipboard
// SetClipboardData 成功即权威成功：不做托管读回、不调 Flush
private static bool TryOpenAndSet(string text) { /* P/Invoke，见源文件 */ }

// 后台 STA 线程包装：UI 侧 async/await，不冻结界面
public static Task<bool> TrySetTextAsync(string text)
{
    var completion = new TaskCompletionSource<bool>();
    var thread = new Thread(() =>
    {
        try { completion.SetResult(TrySetText(text)); }
        catch (Exception exception) { completion.SetException(exception); }
    });
    thread.SetApartmentState(ApartmentState.STA);
    thread.IsBackground = true;
    thread.Start();
    return completion.Task;
}

// 剪切：先存选区 → await 写入 → 复核“仍可编辑且选区未变”才删字
var selectedText = textBox.SelectedText;
bool written = await TrySetTextAsync(selectedText);
if (!written) { /* 弹“被占用”提示 */ return; }
if (!IsEditable(textBox) || textBox.SelectedText != selectedText) return;
textBox.SelectedText = string.Empty;

// 粘贴：托管读取即可（读失败才提示，空剪贴板静默返回）；先记插入点再替换
var insertionStart = textBox.SelectionStart;
textBox.SelectedText = text;
textBox.Select(insertionStart + text.Length, 0); // 光标钉到末尾，消掉选区
```

设计说明（与源文件 `TextBoxClipboard.cs` 对应）：

- 右键菜单在代码里生成（`EnsureContextMenu`），`CommandTarget` 显式指向输入框，不依赖焦点；菜单样式走各项目 App.xaml 的隐式样式。
- 写路径只表达“抢到/没抢到”（返回 bool 重试），不吞其他异常；`SetClipboardData` 成功即权威成功。
- `SelectedText` 读写都是可撤销的常规编辑，不破坏 `Ctrl+Z`；粘贴后光标钉到插入末尾。
- 纯文本场景只用 `CF_UNICODETEXT`；富文本需另行处理格式，不在本模板范围。

## 4. 验证清单

1. 选中文字 → 右键菜单剪切/复制/粘贴可用；未选中时剪切/复制置灰。
2. `Ctrl+X/C/V` 与菜单行为一致。
3. 复制 → 关闭程序 → 到记事本粘贴，内容仍在（原生写入自带落盘）。
4. 远程同步软件常驻时复制/剪切：应成功（最多体感顿一下）或看到明确的“被占用”提示；UI 不冻结。
5. `Ctrl+Z` 能撤销剪切/粘贴。

## 5. 附：本项目实例

- `SeerLauncher/Presentation/Controls/TextBoxClipboard.cs`：附加行为完整实现（单文件移植自 xm-keygen，提示换成项目自研 `MessageDialog`）。
- `SeerLauncher/Presentation/Windows/InputDialog.xaml`：`controls:TextBoxClipboard.Enable="True"` 一行接入（程序内唯一的 `TextBox` 在此）。
