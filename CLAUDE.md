# CLAUDE.md

Windows 桌面 Markdown 预览工具 — WPF (.NET 8) + WebView2 + Markdig。纯查看器，无编辑功能。

详细文档见 `docs/ai-context/`：
- `architecture.md` — 架构、数据流、MarkdownService 解析管道、主题系统、WebView2 通信桥
- `files.md` — 每个源文件的职责、关键成员、内部实现细节
- `conventions.md` — 编码规范、命名空间、事件模式、DynamicResource 规则、持久化约定

## 快速参考

**技术栈**：.NET 8 WPF + WebView2 1.0.3912.50 + Markdig 1.1.3

**架构**：`MainWindow.xaml/.cs` → `MainViewModel`（事件: NavigateRequested / TocScrollRequested / ThemeChanged） → `MarkdownService` / `RecentFilesService` → `TabItem` / `TocHeading` / `RecentFileEntry`

**构建**：`dotnet build` / `dotnet run` / `dotnet publish -c Release`（独立单文件 win-x64）

**关键规则**：
- XAML 颜色必须用 `{DynamicResource}`，18 个画刷键在 Dark.xaml 和 Light.xaml 中必须一致
- CSS 为 EmbeddedResource，通过 `GetManifestResourceStream()` 加载
- WebView2 ↔ WPF 通过 `window.chrome.webview.postMessage()` 通信（toc-scroll / zoom / link-click）
- 持久化目录：`%AppData%/MarkdownViewer/`（settings.json、recent.json、WebView2/）
- 不写 Co-Authored-By，不执行 git push
