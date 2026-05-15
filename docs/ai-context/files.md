# 文件地图

`src/` 下每个源文件的职责和关键细节。

## 根目录（`src/`）

| 文件 | 用途 |
|---|---|
| `MarkdownViewer.csproj` | 项目文件。目标 `net8.0-windows`，PublishSingleFile + SelfContained win-x64。版本 1.1.0。NuGet：Markdig、Microsoft.Web.WebView2。CSS 文件设为 EmbeddedResource。 |
| `App.xaml` | 应用定义。默认加载 `Themes/Dark.xaml` 作为合并字典。全局 Button cursor=Hand。 |
| `App.xaml.cs` | 启动入口：检测主题（settings.json → 注册表 → 默认暗色）。`SwitchTheme(bool)`：热替换 MergedDictionaries[0]。`SaveThemePreference()`：写入 JSON。 |
| `MainWindow.xaml` | 完整 UI 布局：工具栏（打开/最近文件/目录/主题切换）、最近文件弹出菜单、标签栏、目录侧栏、WebView2 内容区、落地页、状态栏。所有颜色使用 `{DynamicResource}`。 |
| `MainWindow.xaml.cs` | Code-behind。WebView2 初始化（自定义 UDF 路径）。事件桥接：VM 事件 → WebView2 导航。WebMessageReceived：分发 toc-scroll/zoom/link-click 消息。键盘快捷键：Ctrl+Tab/Shift+Tab/W。拖放。最近文件弹出菜单切换。 |
| `AssemblyInfo.cs` | WPF 主题信息属性。 |

## Converters（`src/Converters/`）

| 文件 | 用途 |
|---|---|
| `BoolToVisibilityConverter.cs` | `bool`/`int` → `Visibility.Visible`/`Collapsed`。`Invert` 属性可翻转逻辑。用于 ShowTocSidebar、HasTabs、落地页显隐、最近文件"missing"标签。 |
| `InverseBoolConverter.cs` | `bool` → `!bool`。用于 RecentFileEntry.Exists → "missing"标签显隐。 |

## Models（`src/Models/`）

| 文件 | 关键成员 | 备注 |
|---|---|---|
| `TabItem.cs` | `FilePath`、`FileName`、`RawMarkdown`、`HtmlContent`、`TocHeadings`、`IsActive`、`IsModified`、`EncodingName`、`LineCount`、`WordCount` | 继承 ObservableObject。`FileName` 由 `FilePath` 派生。`IsActive` 变更时通知。 |
| `TocHeading.cs` | `Id`、`Text`、`Level`、`IsActive`、`Indent`、`IsVisible` | 继承 ObservableObject。`Indent = (Level-1)*12`。`IsVisible = Level <= 4`。 |
| `RecentFileEntry.cs` | `FilePath`、`LastOpened`、`Exists`、`Directory`、`FileName` | 普通 POCO（非 ObservableObject）。`Exists` 调用 `File.Exists()`。与 JSON 之间序列化/反序列化。 |

## Services（`src/Services/`）

| 文件 | 关键方法 | 备注 |
|---|---|---|
| `MarkdownService.cs` | `Parse(md) → (html, headings)`、`BuildFullHtml(body, isDark) → fullHtml` | 构造函数构建 Markdig pipeline。`Parse()` 统一换行符，调用 `InjectHeadingIds()`，再走 Markdig。`BuildFullHtml()` 从嵌入资源加载 CSS，包装 HTML 和 4 个 JS IIFE。 |
| `RecentFilesService.cs` | `Load() → List<RecentFileEntry>`、`Add(path)`、`Clear()` | 最多 20 条。Add 时去重（移除旧条目，插入到索引 0 处）。JSON 文件位于 `%AppData%/MarkdownViewer/recent.json`。 |

### MarkdownService 内部细节

- `InjectHeadingIds(md, out headings)` — private static。按 `\n` 分割，跟踪围栏代码块状态，匹配 ATX（`^#{1,6}\s+(.+)$`）和 Setext（`===` / `---`）标题，注入 `{#id}` 属性。调用 `StripBracketPrefix()` 和 `StripInlineMarkdown()` 生成干净的目录文本。
- `StripBracketPrefix(text)` — 移除开头的 `[...]` 前缀（如 `[TOC] 我的标题` → `我的标题`）。
- `StripInlineMarkdown(text)` — 移除图片、链接、粗体/斜体、行内代码、HTML 标签，以生成干净的目录展示文本。
- `Slugify(text, existing)` — 生成 URL 安全的 ID：移除中文标点，转小写，非字母数字替换为 `-`。重名时加 `-1`/`-2` 后缀。支持中文字符。
- `LoadCss(resourceName)` — 通过 `GetManifestResourceStream()` 从 assembly 加载嵌入式 CSS。

## ViewModels（`src/ViewModels/`）

| 文件 | 用途 |
|---|---|
| `ObservableObject.cs` | MVVM 基类。`INotifyPropertyChanged` + `SetProperty<T>(ref field, value)` 含相等性检查。使用 `[CallerMemberName]`。 |
| `RelayCommand.cs` | `RelayCommand`（Action）+ `RelayCommand<T>`（Action<T?>）。均使用 `CommandManager.RequerySuggested` 实现 CanExecuteChanged。 |
| `MainViewModel.cs` | 核心 ViewModel — 见下。 |

### MainViewModel 状态和命令

**可观察属性：**
- `Tabs` — `ObservableCollection<TabItem>`
- `TocHeadings` — `ObservableCollection<TocHeading>`（从当前标签页扁平化而来）
- `RecentFiles` — `ObservableCollection<RecentFileEntry>`（从 service 加载）
- `ActiveTab` — setter：取消旧标签激活、激活新标签、触发 NavigateRequested、构建目录
- `IsDarkMode` — setter 触发 ThemeChanged 事件
- `IsTocVisible` / `ShowTocSidebar` — 目录显隐
- `StatusText` / `WindowTitle` / `HasTabs` — 派生的 UI 状态

**命令（RelayCommand）：**
- `OpenFileCommand` — OpenFileDialog（.md 过滤器），委托给 LoadFile()
- `CloseTabCommand` — 移除标签页，选中最后一个剩余标签，若为空则触发 NavigateRequested(null)
- `SelectTabCommand` — 设置 ActiveTab
- `ToggleTocCommand` — 翻转 IsTocVisible
- `OpenRecentFileCommand` — 检查 Exists，调用 LoadFile() 或弹出警告
- `ClearRecentFilesCommand` — 清空 service + collection
- `ToggleThemeCommand` — 翻转 IsDarkMode，调用 App.SwitchTheme()

**事件：**
- `NavigateRequested` — TabItem?（null 表示清空/落地页）
- `TocScrollRequested` — TocHeading（点击目录条目 → WebView2 中滚动）
- `ThemeChanged` — EventArgs（重新渲染所有标签页）

**关键方法：**
- `LoadFile(filePath)` — 编码检测、大小检查（>5MB 警告）、文件类型过滤（.md/.markdown/.txt/.mdown）、去重（如已打开则切换到已有标签页）、解析、创建 TabItem、加入 collection、更新最近文件
- `ReadFileAutoEncoding()` — private static。UTF-8 BOM 检测 → 若前 500 字符中替换字符 >5 个，回退到系统 ANSI 代码页（`Encoding.GetEncoding(0)`）
- `OnWebMessageReceived(headingId)` — 设置匹配 TocHeading 的 IsActive
- `SelectNextTab()` / `SelectPreviousTab()` — 循环切换标签页

## Themes（`src/Themes/`）

| 文件 | 内容 |
|---|---|
| `Dark.xaml` | 18 个 SolidColorBrush 键。深灰系（#1e1e1e–#3a3a3a），蓝色强调 #007acc，绿色活跃圆点 #6a9955。 |
| `Light.xaml` | 相同的 18 个键。白色/浅灰系，蓝色强调相同 #007acc，绿色活跃圆点 #22863a。 |

**18 个画刷键：** WindowBg、SurfaceBg、ButtonBg、ElevatedBg、BorderBrush、AccentBrush、TabActiveBorder、TabBg、TocActiveBg、TocHoverBg、TextPrimary、TextBody、TextDimmed、DotActive、DotNormal、TocHeaderText、TocItemText、StatusBarBg。

## Resources（`src/Resources/`）

| 文件 | 用途 |
|---|---|
| `logo.ico` | 应用图标（窗口标题栏 + 任务栏） |
| `markdown-styles-dark.css` | 渲染 Markdown 的暗色主题（WebView2 中）。类 GitHub 风格。 |
| `markdown-styles-light.css` | 对应的亮色主题。 |

两份 CSS 文件均为 **EmbeddedResource**（非 Content），通过 `GetManifestResourceStream("MarkdownViewer.Resources.markdown-styles-{dark|light}.css")` 加载。

## 文档

```
docs/superpowers/
├── plans/
│   ├── 2026-04-28-markdown-viewer.md       — 原始实现计划
│   └── 2026-04-29-theme-switching.md        — 主题切换实现计划
└── specs/
    ├── 2026-04-28-markdown-viewer-ui-design.md   — UI 设计规格
    └── 2026-04-29-theme-switching-design.md      — 主题切换设计规格
```
