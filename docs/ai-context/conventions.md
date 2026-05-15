# 编码规范与模式

修改本代码库时应遵循的规则和模式。

## WPF / XAML

- **始终使用 `{DynamicResource}`** 引用颜色/画刷。绝不能用 `{StaticResource}` — 主题切换依赖 DynamicResource 的级联更新机制。
- 所有 18 个画刷键必须同时存在于 `Dark.xaml` 和 `Light.xaml` 中。新增画刷时必须两个文件都加。
- `App.xaml` 中的全局按钮样式为所有 Button 元素设置 `Cursor="Hand"`。

## 命名空间

```
MarkdownViewer                    — App、MainWindow
MarkdownViewer.Models             — TabItem、TocHeading、RecentFileEntry
MarkdownViewer.Services           — MarkdownService、RecentFilesService
MarkdownViewer.ViewModels         — MainViewModel、ObservableObject、RelayCommand
MarkdownViewer.Converters         — BoolToVisibilityConverter、InverseBoolConverter
```

## Code-Behind 模式

MainWindow code-behind 刻意保持精简。仅处理：
- WebView2 生命周期（初始化、导航、消息接收）
- 拖放事件
- 需要区分 Ctrl+Tab 和 Ctrl+Shift+Tab 的键盘事件（InputBinding 不擅长在同一 key 上区分不同修饰键组合）
- 需要同时访问 UI 元素和 ViewModel 的点击事件（弹出菜单切换、最近文件点击）

所有状态和逻辑位于 MainViewModel 中。Code-behind 通过 `(MainViewModel)DataContext` 强制转换访问 VM。

## View→ViewModel 解耦事件

MainViewModel 暴露 C# 事件，用于那些 ViewModel 无法独立完成的动作（因为它不依赖 WPF/WebView2）：
- `NavigateRequested` — ViewModel 判断内容变化，code-behind 将 HTML 推入 WebView2
- `TocScrollRequested` — ViewModel 收到目录点击，code-behind 执行 JS `scrollIntoView()`
- `ThemeChanged` — ViewModel 切换主题，code-behind 重新渲染所有标签页

## WebView2 ↔ WPF 桥接

渲染的 HTML 中的 JavaScript 通过以下方式与 WPF 通信：
```js
window.chrome.webview.postMessage({type:'toc-scroll', headingId: current.id});
window.chrome.webview.postMessage({type:'zoom', level: zoom});
window.chrome.webview.postMessage({type:'link-click', url: url});
```

WPF 端在 `CoreWebView2.WebMessageReceived` 中接收 → 解析 JSON 的 `type` 字段 → 分发处理。

WPF 调用 JS：
```csharp
await MarkdownWebView.CoreWebView2.ExecuteScriptAsync("document.body.style.zoom = 1.0;");
```

## 持久化

- `settings.json` — `{"theme": "dark"|"light"}`。启动时读取，切换主题时写入。
- `recent.json` — `[{FilePath, LastOpened}]`。最多 20 条。去重：移除旧条目，新条目插入顶部。
- 两者均存储在 `Path.Combine(Environment.GetFolderPath(SpecialFolder.ApplicationData), "MarkdownViewer", filename)`。
- 使用 `System.Text.Json`（不要用 Newtonsoft）。

## Markdown 处理

- `MarkdownService` 除 Markdig pipeline 外（构造函数中构建一次）是无状态的。
- `Parse()` 在处理前将 `\r\n` 统一为 `\n`。
- `InjectHeadingIds()` 必须跳过围栏代码块，避免 `{#id}` 注入破坏代码。通过跟踪围栏字符和数量来正确检测边界。
- `BuildFullHtml()` 是唯一将 CSS + JS 包装到 body HTML 外围的地方。

## 文件编码

MainViewModel 中的 `ReadFileAutoEncoding()`：
1. `StreamReader(file, UTF8, detectEncodingFromByteOrderMarks: true)`
2. 若当前编码为 UTF8 且前 500 字符中替换字符（`�`）超过 5 个 → 用 `Encoding.GetEncoding(0)`（系统 ANSI 代码页）重试

## ObservableObject 用法

需要变更通知的 Model 继承 `ObservableObject`，使用 `SetProperty(ref field, value)`：
```csharp
private bool _isActive;
public bool IsActive
{
    get => _isActive;
    set => SetProperty(ref _isActive, value);
}
```

`RecentFileEntry` 是普通 POCO（不继承 ObservableObject）—— 它在 ItemsControl 中以静态数据使用。

## 构建与发布

- `dotnet build` — Debug 模式，输出到 `src/bin/Debug/net8.0-windows/`
- `dotnet publish -c Release` — 独立单文件 exe，目标 win-x64。输出：`src/bin/Release/net8.0-windows/win-x64/publish/win-x64/MarkdownViewer.exe`
- 发布配置文件位于 `src/Properties/PublishProfiles/FolderProfile.pubxml`

## Git

- 分支：`master`
- 提交遵循 conventional commits 风格：`feat:`、`fix:`、`refactor:`、`build:`、`chore:`、`docs:`、`style:`
- **不要**在提交中包含 `Co-Authored-By` 尾缀（用户偏好）
- `git push` 永远由用户手动执行，AI 不执行
