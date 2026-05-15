# 架构

## 概述

MarkdownViewer 是一款 Windows 桌面 Markdown 预览工具，纯查看器，无编辑功能。

**核心链路**：WPF 窗口 → MainViewModel 管理状态 → MarkdownService 解析 Markdown / RecentFilesService 持久化 → WebView2 渲染 HTML。

## 技术栈

| 层 | 技术 |
|---|---|
| UI 框架 | WPF (.NET 8.0, `net8.0-windows`) |
| Markdown 解析 | Markdig 1.1.3（`UseAdvancedExtensions`） |
| HTML 渲染 | Microsoft.Web.WebView2 1.0.3912.50（Chromium 内核） |
| 持久化 | System.Text.Json（settings.json、recent.json） |
| 架构模式 | MVVM（手写基类，无第三方框架） |

## MVVM 分层

```
┌──────────────────────────────────────────────────┐
│  View: MainWindow.xaml + MainWindow.xaml.cs       │
│  - XAML 数据绑定到 MainViewModel                   │
│  - Code-behind: WebView2 生命周期、拖放、          │
│    键盘快捷键、事件桥接                            │
└──────────────┬───────────────────────────────────┘
               │ DataContext 绑定
               │ 事件: NavigateRequested、TocScrollRequested、ThemeChanged
┌──────────────▼───────────────────────────────────┐
│  ViewModel: MainViewModel                         │
│  - 全部应用状态（标签页、目录、主题、最近文件）     │
│  - 暴露 RelayCommand + 事件                       │
│  - 调用 Service 层完成解析和持久化                 │
└──────────────┬───────────────────────────────────┘
               │
┌──────────────▼───────────────────────────────────┐
│  Services:                                        │
│  - MarkdownService: Parse() + BuildFullHtml()      │
│  - RecentFilesService: Load/Add/Clear（JSON）     │
└──────────────┬───────────────────────────────────┘
               │
┌──────────────▼───────────────────────────────────┐
│  Models: TabItem、TocHeading、RecentFileEntry     │
│  （前两者继承 ObservableObject，RecentFileEntry    │
│   为普通 POCO，用于 JSON 序列化）                  │
└──────────────────────────────────────────────────┘
```

## 关键通信路径

### 1. 文件打开 → 渲染

```
用户打开文件（Ctrl+O / 拖放 / 最近文件 / 命令行参数）
  → MainViewModel.LoadFile(filePath)
    → ReadFileAutoEncoding(): 检测编码，读取原始 Markdown
    → MarkdownService.Parse(rawMarkdown)
      → InjectHeadingIds(): 正则预处理，向标题注入 {#id}
      → Markdig.ToHtml(): 解析为 HTML
    → new TabItem { RawMarkdown, HtmlContent, TocHeadings }
    → ActiveTab = tab
      → 触发 NavigateRequested 事件
        → MainWindow.OnNavigateRequested()
          → MarkdownService.BuildFullHtml(html, isDark)
            → 包装 CSS <style> + 4 个 JS IIFE
          → WebView2.NavigateToString(fullHtml)
```

### 2. 目录滚动同步

```
WebView2 JS: scroll 事件监听
  → 找到最近的可视标题（offsetTop <= scrollY + 80）
  → postMessage({type:'toc-scroll', headingId: current.id})
    → MainWindow.OnWebMessageReceived()
      → Dispatcher.Invoke → VM.OnWebMessageReceived(headingId)
        → 更新匹配标题的 TocHeading.IsActive
        → WPF 绑定自动高亮目录中的对应条目
      → ScrollTocToActive(): BringIntoView 滚动目录到当前高亮项
```

### 3. 主题切换

```
用户点击主题按钮 → ToggleThemeCommand
  → MainViewModel.ToggleTheme()
    → IsDarkMode = !IsDarkMode
      → 触发 ThemeChanged 事件
        → MainWindow.OnThemeChanged()
          → 用新 CSS 重新解析所有标签页的 RawMarkdown
          → 重新渲染当前标签页的 HTML 到 WebView2
      → App.SwitchTheme(isDark)
        → 热替换 MergedDictionaries[0]（Dark.xaml ↔ Light.xaml）
        → DynamicResource 绑定自动更新所有 WPF 控件
        → 写入 settings.json
```

### 4. 缩放跨标签页保持

```
JS Ctrl+滚轮 → postMessage({type:'zoom', level})
  → MainWindow 存储 _currentZoom（double）
    → 切换标签页时：NavigationCompleted 事件中
      → ExecuteScriptAsync("document.body.style.zoom = {_currentZoom}")
```

## 键盘快捷键

| 快捷键 | 操作 | 处理位置 |
|---|---|---|
| `Ctrl+O` | 打开文件 | XAML InputBinding → OpenFileCommand |
| `Ctrl+Shift+T` | 切换目录侧栏 | XAML InputBinding → ToggleTocCommand |
| `Ctrl+Tab` | 下一个标签页 | MainWindow.OnKeyDown |
| `Ctrl+Shift+Tab` | 上一个标签页 | MainWindow.OnKeyDown |
| `Ctrl+W` | 关闭当前标签页 | MainWindow.OnKeyDown |
| `Ctrl+滚轮` | 缩放 30%-300% | WebView2 JS（wheel 事件） |
| `Ctrl+0` | 重置缩放 100% | WebView2 JS（keydown 事件） |

## MarkdownService 解析管道

```
原始 Markdown 文本
  ↓ InjectHeadingIds() — 正则预处理
  │   • ATX 标题（^#{1,6}\s+.+$）→ 注入 {#slug}
  │   • Setext 标题（=== / ---）→ 在文本行注入 {#slug}
  │   • 跳过围栏代码块（``` / ~~~）
  │   • 剥离内联 Markdown 标记以生成干净的目录文本
  │   • 剥离标题前的 [bracket] 前缀
  │   • 生成唯一 slug，重名时加后缀（-1, -2, ...）
  ↓ Markdig.ToHtml() — UseAdvancedExtensions
  │   （表格、任务列表、删除线、围栏代码块、脚注等）
  ↓ BuildFullHtml(bodyHtml, isDark)
      • 通过 GetManifestResourceStream 加载嵌入式 CSS（暗色或亮色）
      • 包装进 <!DOCTYPE html>，含 4 个 JS IIFE：
        1. 目录滚动跟踪 → postMessage toc-scroll
        2. 代码块复制按钮 → 注入到 <pre> 块中
        3. Ctrl+滚轮缩放（0.3–3.0）+ Ctrl+0 重置 → postMessage zoom
        4. 外部链接点击拦截 → postMessage link-click
```

## 持久化

所有用户数据存储在 `%AppData%/MarkdownViewer/` 中：

| 文件 | 格式 | 内容 |
|---|---|---|
| `settings.json` | JSON `{"theme":"dark"\|"light"}` | 主题偏好 |
| `recent.json` | JSON `[{FilePath, LastOpened}]` | 最近文件（最多 20 条） |
| `WebView2/` | WebView2 UDF | 浏览器缓存、localStorage |

## 主题系统

- 18 个命名 SolidColorBrush 键在 `Themes/Dark.xaml` 和 `Themes/Light.xaml` 中完全相同
- 所有 WPF XAML 使用 `{DynamicResource KeyName}`（不可用 StaticResource）
- 主题切换：替换 `MergedDictionaries[0]` 的 Source → DynamicResource 级联更新所有控件
- CSS 文件为 EmbeddedResource，通过 `GetManifestResourceStream()` 加载，在 BuildFullHtml() 中按主题选择
- 默认主题：读取 settings.json → 回退到 Windows 注册表 `AppsUseLightTheme` → 默认暗色
