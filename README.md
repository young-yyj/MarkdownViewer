# MarkdownViewer

一个基于 WPF (.NET 8) + WebView2 的桌面 Markdown 预览工具。

A desktop Markdown preview tool built with WPF (.NET 8) and WebView2.

## 功能特性 / Features

- **多标签页浏览 / Multi-tab browsing** — 同时打开多个 Markdown 文件 / open multiple Markdown files in parallel
- **暗色/亮色主题 / Dark / Light theme** — 自动检测 Windows 系统主题，支持手动切换 / auto-detects Windows system theme, manual toggle supported
- **目录侧栏 / TOC sidebar** — 从标题自动生成，随内容滚动同步 / auto-generated from headings, scroll-synced with content
- **Ctrl+滚轮缩放 / Ctrl+Wheel zoom** — 缩放范围 30%–300%，切换标签页后保持 / zoom from 30% to 300%, persists across tab switches
- **最近文件 / Recent files** — 记录最近打开的 20 个文件 / remembers up to 20 recently opened files
- **拖拽打开 / Drag & drop** — 将 `.md` 文件直接拖入窗口即可打开 / drag `.md` files directly onto the window
- **代码块复制 / Code block copy** — 每个代码块都有一键复制按钮 / one-click copy button on every code block
- **阅读时间估算 / Read time estimate** — 状态栏显示预估阅读时长 / displayed in the status bar
- **大文件提示 / File size warning** — 超过 5MB 的文件打开前弹出确认 / prompts for files over 5MB
- **单实例运行 / Single instance** — 新启动实例将文件路径转发到已有窗口 / new instances forward file paths to the existing window

## 技术栈 / Tech Stack

- [.NET 8](https://dotnet.microsoft.com/) + WPF
- [Microsoft WebView2](https://learn.microsoft.com/en-us/microsoft-edge/webview2/) — Chromium 内核渲染 / Chromium-based rendering
- [Markdig](https://github.com/xoofx/markdig) — Markdown 解析引擎，启用高级扩展 / Markdown parsing with advanced extensions

## 快速开始 / Getting Started

```bash
# 编译构建 / Build
dotnet build

# 发布（独立单文件，win-x64）/ Publish (self-contained single exe, win-x64)
dotnet publish -c Release
```

发布输出路径 / Published output: `publish/win-x64/MarkdownViewer.exe`

## 快捷键 / Keyboard Shortcuts

| 快捷键 / Keys | 操作 / Action |
|---|---|
| `Ctrl + O` | 打开文件 / Open file |
| `Ctrl + W` | 关闭当前标签页 / Close current tab |
| `Ctrl + Tab` | 下一个标签页 / Next tab |
| `Ctrl + Shift + Tab` | 上一个标签页 / Previous tab |
| `Ctrl + Shift + T` | 切换目录侧栏 / Toggle TOC sidebar |
| `Ctrl + Wheel` | 放大/缩小 / Zoom in/out |
| `Ctrl + 0` | 重置缩放为 100% / Reset zoom to 100% |

## 项目结构 / Project Structure

```
MarkdownViewer/
└── src/
    ├── App.xaml.cs           — 应用入口，单实例检测、主题初始化 / Entry, single-instance, theme
    ├── MainWindow.xaml/.cs   — 主窗口 UI 及事件处理 / Main window UI and event handling
    ├── Converters/           — WPF 值转换器 / WPF value converters
    ├── Models/               — 数据模型 (TabItem, TocHeading, RecentFileEntry) / Data models
    ├── Services/             — MarkdownService (解析), RecentFilesService (最近文件)
    ├── ViewModels/           — MainViewModel, RelayCommand, ObservableObject
    ├── Resources/            — 内嵌 CSS (暗色/亮色), logo.ico / Embedded CSS (dark/light), logo.ico
    └── Themes/               — WPF 资源字典 (Dark.xaml, Light.xaml) / WPF resource dictionaries
```

## 许可证 / License

[MIT](LICENSE)
