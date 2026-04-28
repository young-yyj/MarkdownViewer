# Markdown Viewer Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build a WPF desktop Markdown viewer with tabbed file management, TOC navigation, and Dark Dev theme.

**Architecture:** MVVM pattern with WPF + WebView2 for HTML-based Markdown rendering. Markdig parses Markdown to HTML; WebView2 displays it with Dark Dev CSS. A WPF-native sidebar renders the TOC with JS-mediated scroll tracking between WebView2 and WPF.

**Tech Stack:** .NET 8.0 WPF, Markdig (MD parsing), Microsoft.Web.WebView2 (HTML display)

---

### Task 1: Project setup — NuGet packages and directory structure

**Files:**
- Modify: `MarkdownViewer.csproj`
- Create: `Models/` (dir), `Services/` (dir), `ViewModels/` (dir), `Converters/` (dir), `Resources/` (dir)

- [ ] **Step 1: Add NuGet packages**

```bash
cd "D:/Desktop/我的项目/MarkdownViewer"
dotnet add package Markdig
dotnet add package Microsoft.Web.WebView2
```

- [ ] **Step 2: Create directory structure**

```bash
mkdir -p Models Services ViewModels Converters Resources
```

- [ ] **Step 3: Restore and build to verify packages resolve**

```bash
dotnet restore && dotnet build
```

Expected: Build succeeds with 0 errors.

- [ ] **Step 4: Commit**

```bash
git add MarkdownViewer.csproj Models Services ViewModels Converters Resources
git commit -m "chore: add Markdig and WebView2 packages, create project directory structure"
```

---

### Task 2: ObservableObject and RelayCommand base classes

**Files:**
- Create: `ViewModels/ObservableObject.cs`
- Create: `ViewModels/RelayCommand.cs`

- [ ] **Step 1: Create ObservableObject**

```csharp
// ViewModels/ObservableObject.cs
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MarkdownViewer.ViewModels;

public class ObservableObject : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}
```

- [ ] **Step 2: Create RelayCommand and RelayCommand<T>**

```csharp
// ViewModels/RelayCommand.cs
using System.Windows.Input;

namespace MarkdownViewer.ViewModels;

public class RelayCommand : ICommand
{
    private readonly Action _execute;
    private readonly Func<bool>? _canExecute;

    public RelayCommand(Action execute, Func<bool>? canExecute = null)
    {
        _execute = execute;
        _canExecute = canExecute;
    }

    public event EventHandler? CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }

    public bool CanExecute(object? parameter) => _canExecute?.Invoke() ?? true;
    public void Execute(object? parameter) => _execute();
}

public class RelayCommand<T> : ICommand
{
    private readonly Action<T?> _execute;
    private readonly Func<T?, bool>? _canExecute;

    public RelayCommand(Action<T?> execute, Func<T?, bool>? canExecute = null)
    {
        _execute = execute;
        _canExecute = canExecute;
    }

    public event EventHandler? CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }

    public bool CanExecute(object? parameter) => _canExecute?.Invoke((T?)parameter) ?? true;
    public void Execute(object? parameter) => _execute((T?)parameter);
}
```

- [ ] **Step 3: Build to verify both files compile**

```bash
dotnet build
```

Expected: Build succeeds.

- [ ] **Step 4: Commit**

```bash
git add ViewModels/ObservableObject.cs ViewModels/RelayCommand.cs
git commit -m "feat: add ObservableObject and RelayCommand MVVM base classes"
```

---

### Task 3: Data models — TabItem, TocHeading, RecentFileEntry

**Files:**
- Create: `Models/TabItem.cs`
- Create: `Models/TocHeading.cs`
- Create: `Models/RecentFileEntry.cs`

- [ ] **Step 1: Create TabItem**

```csharp
// Models/TabItem.cs
namespace MarkdownViewer.Models;

public class TabItem
{
    public string FilePath { get; set; } = string.Empty;

    public string FileName => string.IsNullOrEmpty(FilePath)
        ? "Untitled"
        : System.IO.Path.GetFileName(FilePath);

    public string? RawMarkdown { get; set; }
    public string? HtmlContent { get; set; }
    public List<TocHeading> TocHeadings { get; set; } = new();
    public bool IsModified { get; set; }
    public string EncodingName { get; set; } = "UTF-8";
    public int LineCount { get; set; }
    public int WordCount { get; set; }
}
```

- [ ] **Step 2: Create TocHeading**

```csharp
// Models/TocHeading.cs
using MarkdownViewer.ViewModels;

namespace MarkdownViewer.Models;

public class TocHeading : ObservableObject
{
    public string Id { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public int Level { get; set; }

    public int Indent => (Level - 1) * 12;
    public bool IsVisible => Level <= 4;

    private bool _isActive;
    public bool IsActive
    {
        get => _isActive;
        set => SetProperty(ref _isActive, value);
    }
}
```

- [ ] **Step 3: Create RecentFileEntry**

```csharp
// Models/RecentFileEntry.cs
namespace MarkdownViewer.Models;

public class RecentFileEntry
{
    public string FilePath { get; set; } = string.Empty;
    public DateTime LastOpened { get; set; }

    public bool Exists => System.IO.File.Exists(FilePath);
    public string Directory => System.IO.Path.GetDirectoryName(FilePath) ?? string.Empty;
    public string FileName => System.IO.Path.GetFileName(FilePath);
}
```

- [ ] **Step 4: Build to verify**

```bash
dotnet build
```

Expected: Build succeeds.

- [ ] **Step 5: Commit**

```bash
git add Models/
git commit -m "feat: add TabItem, TocHeading, and RecentFileEntry models"
```

---

### Task 4: Value converters for WPF bindings

**Files:**
- Create: `Converters/BoolToVisibilityConverter.cs`
- Create: `Converters/InverseBoolConverter.cs`

- [ ] **Step 1: Create BoolToVisibilityConverter**

```csharp
// Converters/BoolToVisibilityConverter.cs
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace MarkdownViewer.Converters;

public class BoolToVisibilityConverter : IValueConverter
{
    public bool Invert { get; set; }

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var boolValue = value is bool b && b;
        if (Invert) boolValue = !boolValue;
        return boolValue ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is Visibility v && v == Visibility.Visible ? !Invert : Invert;
    }
}
```

- [ ] **Step 2: Create InverseBoolConverter**

```csharp
// Converters/InverseBoolConverter.cs
using System.Globalization;
using System.Windows.Data;

namespace MarkdownViewer.Converters;

public class InverseBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is bool b && !b;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is bool b && !b;
    }
}
```

- [ ] **Step 3: Build and commit**

```bash
dotnet build && git add Converters/ && git commit -m "feat: add WPF value converters for bool-to-visibility and inverse"
```

---

### Task 5: RecentFilesService — load, save, add, clear

**Files:**
- Create: `Services/RecentFilesService.cs`

- [ ] **Step 1: Create RecentFilesService**

```csharp
// Services/RecentFilesService.cs
using System.Text.Json;
using MarkdownViewer.Models;

namespace MarkdownViewer.Services;

public class RecentFilesService
{
    private const int MaxEntries = 20;
    private readonly string _dataFile;

    public RecentFilesService()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var dir = System.IO.Path.Combine(appData, "MarkdownViewer");
        System.IO.Directory.CreateDirectory(dir);
        _dataFile = System.IO.Path.Combine(dir, "recent.json");
    }

    public List<RecentFileEntry> Load()
    {
        if (!System.IO.File.Exists(_dataFile))
            return new List<RecentFileEntry>();

        var json = System.IO.File.ReadAllText(_dataFile);
        return JsonSerializer.Deserialize<List<RecentFileEntry>>(json) ?? new List<RecentFileEntry>();
    }

    public void Add(string filePath)
    {
        var entries = Load();
        var existing = entries.FirstOrDefault(e => e.FilePath == filePath);
        if (existing != null)
            entries.Remove(existing);

        entries.Insert(0, new RecentFileEntry
        {
            FilePath = filePath,
            LastOpened = DateTime.Now
        });

        if (entries.Count > MaxEntries)
            entries = entries.Take(MaxEntries).ToList();

        Save(entries);
    }

    public void Clear()
    {
        if (System.IO.File.Exists(_dataFile))
            System.IO.File.Delete(_dataFile);
    }

    private void Save(List<RecentFileEntry> entries)
    {
        var json = JsonSerializer.Serialize(entries, new JsonSerializerOptions { WriteIndented = true });
        System.IO.File.WriteAllText(_dataFile, json);
    }
}
```

- [ ] **Step 2: Build and commit**

```bash
dotnet build && git add Services/RecentFilesService.cs && git commit -m "feat: add RecentFilesService for persisted recent file tracking"
```

---

### Task 6: MarkdownService — parse MD to HTML with heading ID injection

**Files:**
- Create: `Services/MarkdownService.cs`

- [ ] **Step 1: Create MarkdownService**

```csharp
// Services/MarkdownService.cs
using System.Text.RegularExpressions;
using Markdig;

namespace MarkdownViewer.Services;

public class MarkdownService
{
    private readonly MarkdownPipeline _pipeline;

    public MarkdownService()
    {
        _pipeline = new MarkdownPipelineBuilder()
            .UseAdvancedExtensions()
            .Build();
    }

    public (string html, List<(string id, string text, int level)> headings) Parse(string markdown)
    {
        var processed = InjectHeadingIds(markdown, out var headings);
        var html = Markdown.ToHtml(processed, _pipeline);
        return (html, headings);
    }

    public string BuildFullHtml(string bodyHtml, string css)
    {
        return $@"<!DOCTYPE html>
<html lang=""en"">
<head>
<meta charset=""UTF-8"">
<style>{css}</style>
</head>
<body>
<div class=""markdown-body"">
{bodyHtml}
</div>
<script>
(function() {{
    const headings = document.querySelectorAll('.markdown-body h1, .markdown-body h2, .markdown-body h3, .markdown-body h4');
    if (headings.length === 0) return;
    function updateActive() {{
        let current = headings[0];
        const top = window.scrollY + 80;
        for (const h of headings) {{
            if (h.offsetTop <= top) current = h;
        }}
        window.chrome.webview.postMessage({{type:'toc-scroll', headingId: current.id}});
    }}
    window.addEventListener('scroll', updateActive, {{passive: true}});
    updateActive();
}})();
</script>
</body>
</html>";
    }

    private static string InjectHeadingIds(string markdown, out List<(string id, string text, int level)> headings)
    {
        headings = new();
        var lines = markdown.Split('\n');
        for (int i = 0; i < lines.Length; i++)
        {
            var match = Regex.Match(lines[i], @"^(#{1,6})\s+(.+)$");
            if (!match.Success) continue;

            var level = match.Groups[1].Length;
            var text = match.Groups[2].Value.Trim();
            var id = Slugify(text, headings);
            headings.Add((id, text, level));
            lines[i] = $"{match.Groups[1].Value} {{#{id}}} {text}";
        }
        return string.Join('\n', lines);
    }

    private static string Slugify(string text, List<(string id, string, int)> existing)
    {
        var id = Regex.Replace(text.ToLowerInvariant(), @"[^a-z0-9一-鿿]+", "-").Trim('-');
        if (string.IsNullOrEmpty(id)) id = "heading";
        var original = id;
        int suffix = 1;
        while (existing.Any(h => h.id == id))
            id = $"{original}-{suffix++}";
        return id;
    }
}
```

- [ ] **Step 2: Build and commit**

```bash
dotnet build && git add Services/MarkdownService.cs && git commit -m "feat: add MarkdownService with Markdig pipeline and heading ID injection"
```

---

### Task 7: MainViewModel — core properties, collections, and file loading

**Files:**
- Create: `ViewModels/MainViewModel.cs`

- [ ] **Step 1: Create MainViewModel with properties and file-loading logic**

```csharp
// ViewModels/MainViewModel.cs
using System.Collections.ObjectModel;
using System.Text;
using System.Windows;
using MarkdownViewer.Models;
using MarkdownViewer.Services;

namespace MarkdownViewer.ViewModels;

public class MainViewModel : ObservableObject
{
    private readonly RecentFilesService _recentFiles;
    private readonly MarkdownService _markdown;

    public MainViewModel()
    {
        _recentFiles = new RecentFilesService();
        _markdown = new MarkdownService();

        OpenFileCommand = new RelayCommand(OpenFile);
        CloseTabCommand = new RelayCommand<TabItem>(CloseTab, _ => ActiveTab != null);
        SelectTabCommand = new RelayCommand<TabItem>(SelectTab);
        ToggleTocCommand = new RelayCommand(ToggleToc);
        OpenRecentFileCommand = new RelayCommand<RecentFileEntry>(OpenRecentFile);
        ClearRecentFilesCommand = new RelayCommand(ClearRecentFiles);

        LoadRecentFiles();
    }

    // ── Collections ──────────────────────────────────

    public ObservableCollection<TabItem> Tabs { get; } = new();
    public ObservableCollection<TocHeading> TocHeadings { get; } = new();
    public ObservableCollection<RecentFileEntry> RecentFiles { get; } = new();

    // ── Properties ───────────────────────────────────

    private TabItem? _activeTab;
    public TabItem? ActiveTab
    {
        get => _activeTab;
        set
        {
            if (SetProperty(ref _activeTab, value))
            {
                OnPropertyChanged(nameof(HasTabs));
                OnPropertyChanged(nameof(WindowTitle));
                BuildTocForActiveTab();
                NavigateRequested?.Invoke(this, value);
            }
        }
    }

    private bool _isTocVisible = true;
    public bool IsTocVisible
    {
        get => _isTocVisible;
        set => SetProperty(ref _isTocVisible, value);
    }

    private string _statusText = "Ready";
    public string StatusText
    {
        get => _statusText;
        set => SetProperty(ref _statusText, value);
    }

    public bool HasTabs => Tabs.Count > 0;
    public string WindowTitle => ActiveTab != null
        ? $"{ActiveTab.FileName} — MarkdownViewer"
        : "MarkdownViewer";

    // ── Commands ─────────────────────────────────────

    public RelayCommand OpenFileCommand { get; }
    public RelayCommand<TabItem> CloseTabCommand { get; }
    public RelayCommand<TabItem> SelectTabCommand { get; }
    public RelayCommand ToggleTocCommand { get; }
    public RelayCommand<RecentFileEntry> OpenRecentFileCommand { get; }
    public RelayCommand ClearRecentFilesCommand { get; }

    // ── Events ───────────────────────────────────────

    public event EventHandler<TabItem?>? NavigateRequested;
    public event EventHandler<TocHeading>? TocScrollRequested;

    // ── File Operations ──────────────────────────────

    public void OpenFile()
    {
        var dlg = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "Markdown files (*.md)|*.md|All files (*.*)|*.*",
            Title = "Open Markdown File"
        };

        if (dlg.ShowDialog() == true)
            LoadFile(dlg.FileName);
    }

    private void OpenRecentFile(RecentFileEntry? entry)
    {
        if (entry == null) return;
        if (entry.Exists)
            LoadFile(entry.FilePath);
        else
            MessageBox.Show($"File not found:\n{entry.FilePath}", "File Missing",
                MessageBoxButton.OK, MessageBoxImage.Warning);
    }

    public void LoadFile(string filePath)
    {
        if (!System.IO.File.Exists(filePath))
        {
            MessageBox.Show($"File not found:\n{filePath}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        var ext = System.IO.Path.GetExtension(filePath).ToLowerInvariant();
        if (ext != ".md" && ext != ".markdown" && ext != ".txt" && ext != ".mdown")
        {
            MessageBox.Show("Unsupported file type. Please open a Markdown (.md) or text file.",
                "Unsupported File", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var info = new System.IO.FileInfo(filePath);
        if (info.Length > 5 * 1024 * 1024)
        {
            var result = MessageBox.Show(
                "This file is over 5MB and may be slow to render. Open anyway?",
                "Large File Warning", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result != MessageBoxResult.Yes) return;
        }

        var rawMarkdown = System.IO.File.ReadAllText(filePath, Encoding.UTF8);
        var lineCount = rawMarkdown.Split('\n').Length;
        var wordCount = rawMarkdown.Split(new[] { ' ', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).Length;

        var (html, headingData) = _markdown.Parse(rawMarkdown);

        var tab = new TabItem
        {
            FilePath = filePath,
            RawMarkdown = rawMarkdown,
            HtmlContent = html,
            LineCount = lineCount,
            WordCount = wordCount
        };

        foreach (var (id, text, level) in headingData)
        {
            var tocHeading = new TocHeading { Id = id, Text = text, Level = level };
            tab.TocHeadings.Add(tocHeading);
        }

        Tabs.Add(tab);
        ActiveTab = tab;
        _recentFiles.Add(filePath);
        LoadRecentFiles();
        StatusText = $"✅ {wordCount} words · {EstimateReadTime(wordCount)} min read";

        OnPropertyChanged(nameof(HasTabs));
        OnPropertyChanged(nameof(WindowTitle));
    }

    // ── Tab Operations ───────────────────────────────

    private void CloseTab(TabItem? tab)
    {
        if (tab == null) return;
        Tabs.Remove(tab);

        if (ActiveTab == tab)
            ActiveTab = Tabs.LastOrDefault();

        if (Tabs.Count == 0)
        {
            TocHeadings.Clear();
            NavigateRequested?.Invoke(this, null);
        }

        OnPropertyChanged(nameof(HasTabs));
        OnPropertyChanged(nameof(WindowTitle));
        StatusText = Tabs.Count > 0 ? StatusText : "Ready";
    }

    private void SelectTab(TabItem? tab)
    {
        if (tab != null)
            ActiveTab = tab;
    }

    public void CloseCurrentTab()
    {
        if (ActiveTab != null)
            CloseTab(ActiveTab);
    }

    public void SelectNextTab()
    {
        if (Tabs.Count <= 1) return;
        var idx = Tabs.IndexOf(ActiveTab!);
        idx = (idx + 1) % Tabs.Count;
        ActiveTab = Tabs[idx];
    }

    public void SelectPreviousTab()
    {
        if (Tabs.Count <= 1) return;
        var idx = Tabs.IndexOf(ActiveTab!);
        idx = (idx - 1 + Tabs.Count) % Tabs.Count;
        ActiveTab = Tabs[idx];
    }

    // ── TOC ──────────────────────────────────────────

    private void ToggleToc()
    {
        IsTocVisible = !IsTocVisible;
    }

    private void BuildTocForActiveTab()
    {
        TocHeadings.Clear();
        if (ActiveTab?.TocHeadings == null) return;

        foreach (var h in ActiveTab.TocHeadings.Where(h => h.IsVisible))
            TocHeadings.Add(h);
    }

    public void OnTocHeadingClicked(TocHeading heading)
    {
        TocScrollRequested?.Invoke(this, heading);
    }

    public void OnWebMessageReceived(string headingId)
    {
        foreach (var h in TocHeadings)
            h.IsActive = h.Id == headingId;
    }

    // ── Recent Files ─────────────────────────────────

    private void LoadRecentFiles()
    {
        RecentFiles.Clear();
        foreach (var entry in _recentFiles.Load())
            RecentFiles.Add(entry);
    }

    private void ClearRecentFiles()
    {
        _recentFiles.Clear();
        RecentFiles.Clear();
    }

    // ── Helpers ──────────────────────────────────────

    private static string EstimateReadTime(int wordCount)
    {
        var minutes = Math.Max(1, (int)Math.Ceiling(wordCount / 200.0));
        return minutes.ToString();
    }

    public void OnFileDropped(string[] files)
    {
        foreach (var file in files)
        {
            var ext = System.IO.Path.GetExtension(file).ToLowerInvariant();
            if (ext == ".md" || ext == ".markdown" || ext == ".txt" || ext == ".mdown")
                LoadFile(file);
        }
    }
}
```

- [ ] **Step 2: Build and commit**

```bash
dotnet build && git add ViewModels/MainViewModel.cs && git commit -m "feat: add MainViewModel with file management, tabs, TOC, and commands"
```

---

### Task 8: Dark Dev CSS for Markdown rendering

**Files:**
- Create: `Resources/markdown-styles.css`

- [ ] **Step 1: Create CSS file**

```css
/* Resources/markdown-styles.css */
* { box-sizing: border-box; margin: 0; padding: 0; }

body {
    font-family: 'Segoe UI', -apple-system, sans-serif;
    font-size: 14px;
    line-height: 1.7;
    color: #c8c8c8;
    background-color: #1e1e1e;
    padding: 20px 32px;
}

.markdown-body { max-width: 780px; margin: 0 auto; }

.markdown-body h1 {
    font-size: 26px; font-weight: 700; color: #e0e0e0;
    margin: 0 0 4px 0; padding-bottom: 6px;
    border-bottom: 1px solid #444;
}
.markdown-body h1:not(:first-child) { margin-top: 32px; }

.markdown-body h2 {
    font-size: 20px; font-weight: 600; color: #dcdcdc;
    margin: 24px 0 8px 0; padding-bottom: 2px;
    border-bottom: 1px solid #3a3a3a;
}

.markdown-body h3 {
    font-size: 17px; font-weight: 600; color: #d4d4d4;
    margin: 20px 0 6px 0;
}

.markdown-body h4 {
    font-size: 15px; font-weight: 600; color: #ccc;
    margin: 16px 0 4px 0;
}

.markdown-body h5, .markdown-body h6 {
    font-size: 14px; color: #bbb; margin: 12px 0 4px 0;
}

.markdown-body p { margin: 0 0 14px 0; }

.markdown-body strong { color: #e0e0e0; }
.markdown-body em { color: #d0d0d0; }
.markdown-body del { color: #888; }

.markdown-body a {
    color: #569cd6; text-decoration: none;
}
.markdown-body a:hover { text-decoration: underline; }

.markdown-body code {
    font-family: 'Cascadia Code', 'Consolas', monospace;
    font-size: 13px;
    background: #3a3a3a;
    padding: 2px 5px;
    border-radius: 3px;
    color: #dcdcaa;
}

.markdown-body pre {
    background: #1a1a1a;
    border: 1px solid #333;
    border-radius: 6px;
    padding: 14px 16px;
    margin: 0 0 16px 0;
    overflow-x: auto;
}

.markdown-body pre code {
    background: none;
    padding: 0;
    border-radius: 0;
    color: #d4d4d4;
    font-size: 13px;
    line-height: 1.6;
}

.markdown-body blockquote {
    border-left: 3px solid #569cd6;
    background: #252525;
    padding: 10px 14px;
    margin: 0 0 16px 0;
    border-radius: 0 4px 4px 0;
    color: #b0b0b0;
}

.markdown-body ul, .markdown-body ol {
    padding-left: 22px;
    margin: 0 0 14px 0;
}
.markdown-body li { margin-bottom: 4px; }

.markdown-body table {
    width: 100%;
    border-collapse: collapse;
    margin: 0 0 18px 0;
}
.markdown-body th {
    text-align: left;
    padding: 7px 12px;
    border: 1px solid #444;
    background: #2d2d2d;
    color: #dcdcdc;
    font-weight: 600;
}
.markdown-body td {
    padding: 6px 12px;
    border: 1px solid #3a3a3a;
}
.markdown-body tr:nth-child(even) td { background: #252525; }

.markdown-body hr {
    border: none;
    border-bottom: 2px solid #3a3a3a;
    margin: 20px 0;
}

.markdown-body img {
    max-width: 100%;
    border-radius: 4px;
    margin: 8px 0;
}

.markdown-body input[type="checkbox"] {
    margin-right: 6px;
    accent-color: #6a9955;
}

.markdown-body .task-list-item {
    list-style: none;
    margin-left: -22px;
}
```

- [ ] **Step 2: Set CSS as embedded resource**

Read `MarkdownViewer.csproj`, add this `<ItemGroup>`:

```xml
<ItemGroup>
  <EmbeddedResource Include="Resources\markdown-styles.css" />
</ItemGroup>
```

- [ ] **Step 3: Build and commit**

```bash
dotnet build && git add Resources/markdown-styles.css MarkdownViewer.csproj && git commit -m "feat: add Dark Dev CSS theme for markdown rendering"
```

---

### Task 9: MainWindow.xaml — full UI layout

**Files:**
- Modify: `MainWindow.xaml`

- [ ] **Step 1: Write complete XAML layout**

```xml
<Window x:Class="MarkdownViewer.MainWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:local="clr-namespace:MarkdownViewer"
        xmlns:vm="clr-namespace:MarkdownViewer.ViewModels"
        xmlns:models="clr-namespace:MarkdownViewer.Models"
        xmlns:conv="clr-namespace:MarkdownViewer.Converters"
        Title="{Binding WindowTitle}"
        Height="700" Width="1100"
        MinHeight="500" MinWidth="800"
        Background="#1e1e1e"
        WindowStartupLocation="CenterScreen">

    <Window.DataContext>
        <vm:MainViewModel/>
    </Window.DataContext>

    <Window.Resources>
        <conv:BoolToVisibilityConverter x:Key="BoolToVis" Invert="False"/>
        <conv:BoolToVisibilityConverter x:Key="InverseBoolToVis" Invert="True"/>
        <conv:InverseBoolConverter x:Key="InverseBool"/>
    </Window.Resources>

    <Window.InputBindings>
        <KeyBinding Command="{Binding OpenFileCommand}" Key="O" Modifiers="Ctrl"/>
        <KeyBinding Command="{Binding ToggleTocCommand}" Key="T" Modifiers="Ctrl+Shift"/>
        <KeyBinding Key="W" Modifiers="Ctrl">
            <KeyBinding.Command>
                <vm:RelayCommand>
                    <vm:RelayCommand.ExecuteDelegate>
                        <![CDATA[CloseCurrentTab]]>
                    </vm:RelayCommand.ExecuteDelegate>
                </vm:RelayCommand>
            </KeyBinding.Command>
        </KeyBinding>
    </Window.InputBindings>

    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>  <!-- Title + Toolbar -->
            <RowDefinition Height="Auto"/>  <!-- Tab bar -->
            <RowDefinition Height="*"/>     <!-- Main content -->
            <RowDefinition Height="Auto"/>  <!-- Status bar -->
        </Grid.RowDefinitions>

        <!-- Row 0: Toolbar -->
        <Border Grid.Row="0" Background="#252525" BorderBrush="#333" BorderThickness="0,0,0,1"
                Padding="8,6">
            <Grid>
                <StackPanel Orientation="Horizontal" HorizontalAlignment="Left">
                    <Button Content="📂 Open" Command="{Binding OpenFileCommand}"
                            Background="#3a3a3a" Foreground="#dcdcdc"
                            BorderThickness="0" Padding="10,5" Cursor="Hand"
                            FontSize="12">
                        <Button.Resources>
                            <Style TargetType="Border">
                                <Setter Property="CornerRadius" Value="4"/>
                            </Style>
                        </Button.Resources>
                    </Button>

                    <Separator Style="{StaticResource {x:Static ToolBar.SeparatorStyleKey}}"
                               Margin="4,0"/>

                    <Button x:Name="RecentButton"
                            Background="#3a3a3a" Foreground="#dcdcdc"
                            BorderThickness="0" Padding="8,5" Cursor="Hand"
                            FontSize="12">
                        <Button.Resources>
                            <Style TargetType="Border">
                                <Setter Property="CornerRadius" Value="4"/>
                            </Style>
                        </Button.Resources>
                        <StackPanel Orientation="Horizontal">
                            <TextBlock Text="🕐 " Foreground="#888"/>
                            <TextBlock Text="Recent ▾" Foreground="#dcdcdc"/>
                        </StackPanel>
                    </Button>

                    <Separator Style="{StaticResource {x:Static ToolBar.SeparatorStyleKey}}"
                               Margin="4,0"/>

                    <Button Command="{Binding ToggleTocCommand}"
                            Background="#3a3a3a" Foreground="#dcdcdc"
                            BorderThickness="0" Padding="10,5" Cursor="Hand"
                            FontSize="12">
                        <Button.Resources>
                            <Style TargetType="Border">
                                <Setter Property="CornerRadius" Value="4"/>
                            </Style>
                        </Button.Resources>
                        <StackPanel Orientation="Horizontal">
                            <TextBlock Text="☰ TOC"/>
                        </StackPanel>
                    </Button>
                </StackPanel>

                <TextBlock HorizontalAlignment="Right" VerticalAlignment="Center"
                           Text="{Binding StatusText}"
                           Foreground="#666" FontSize="11"/>
            </Grid>
        </Border>

        <!-- Recent Files Popup -->
        <Popup x:Name="RecentPopup" PlacementTarget="{Binding ElementName=RecentButton}"
               Placement="Bottom" StaysOpen="False" PopupAnimation="Fade"
               Width="340" AllowsTransparency="True">
            <Border Background="#2d2d2d" BorderBrush="#555" BorderThickness="1"
                    CornerRadius="6" Padding="4">
                <StackPanel>
                    <TextBlock Text="RECENT FILES" Foreground="#666" FontSize="10"
                               Margin="8,4" FontWeight="SemiBold"/>
                    <ItemsControl ItemsSource="{Binding RecentFiles}">
                        <ItemsControl.ItemTemplate>
                            <DataTemplate DataType="models:RecentFileEntry">
                                <Border Padding="8,6" Margin="2,0" CornerRadius="4"
                                        Background="Transparent"
                                        MouseLeftButtonDown="RecentFile_Click"
                                        Cursor="Hand">
                                    <Grid>
                                        <Grid.ColumnDefinitions>
                                            <ColumnDefinition Width="Auto"/>
                                            <ColumnDefinition Width="*"/>
                                            <ColumnDefinition Width="Auto"/>
                                        </Grid.ColumnDefinitions>
                                        <TextBlock Grid.Column="0" Text="📄" Margin="0,0,8,0"
                                                   Foreground="#888" FontSize="12"/>
                                        <StackPanel Grid.Column="1">
                                            <TextBlock Text="{Binding FileName}"
                                                       Foreground="#dcdcdc" FontSize="12"/>
                                            <TextBlock Text="{Binding Directory}"
                                                       Foreground="#666" FontSize="10"/>
                                        </StackPanel>
                                        <TextBlock Grid.Column="2"
                                                   Text="missing" FontSize="9"
                                                   Foreground="#d16969"
                                                   Visibility="{Binding Exists,
                                                       Converter={StaticResource InverseBoolToVis}}"
                                                   VerticalAlignment="Center"/>
                                    </Grid>
                                </Border>
                            </DataTemplate>
                        </ItemsControl.ItemTemplate>
                    </ItemsControl>
                    <Separator Background="#444" Margin="4,2"/>
                    <Button Content="Clear recent files"
                            Background="Transparent" Foreground="#888"
                            BorderThickness="0" HorizontalAlignment="Left"
                            FontSize="11" Padding="8,4" Cursor="Hand"
                            Command="{Binding ClearRecentFilesCommand}"/>
                </StackPanel>
            </Border>
        </Popup>

        <!-- Row 1: Tab Bar -->
        <Border Grid.Row="1" Background="#1e1e1e" BorderBrush="#007acc"
                BorderThickness="0,0,0,2" Padding="0">
            <Grid>
                <ScrollViewer HorizontalScrollBarVisibility="Auto"
                              VerticalScrollBarVisibility="Disabled">
                    <StackPanel Orientation="Horizontal" x:Name="TabPanel">
                        <ItemsControl ItemsSource="{Binding Tabs}">
                            <ItemsControl.ItemsPanel>
                                <ItemsPanelTemplate>
                                    <StackPanel Orientation="Horizontal"/>
                                </ItemsPanelTemplate>
                            </ItemsControl.ItemsPanel>
                            <ItemsControl.ItemTemplate>
                                <DataTemplate DataType="models:TabItem">
                                    <Border Padding="10,6" MinWidth="120"
                                            Background="#252525"
                                            BorderBrush="#007acc"
                                            BorderThickness="0,0,0,2"
                                            Margin="0,0,0,-2"
                                            Cursor="Hand"
                                            MouseLeftButtonDown="Tab_Click"
                                            MouseMiddleButtonDown="Tab_MiddleClick"
                                            Tag="{Binding}">
                                        <Border.Resources>
                                            <Style TargetType="Border">
                                                <Style.Triggers>
                                                    <DataTrigger Binding="{Binding IsActive}"
                                                                 Value="True">
                                                        <Setter Property="Background" Value="#2d2d2d"/>
                                                    </DataTrigger>
                                                </Style.Triggers>
                                            </Style>
                                        </Border.Resources>
                                        <StackPanel Orientation="Horizontal">
                                            <Ellipse Width="6" Height="6"
                                                     Margin="0,0,6,0" VerticalAlignment="Center">
                                                <Ellipse.Style>
                                                    <Style TargetType="Ellipse">
                                                        <Style.Triggers>
                                                            <DataTrigger Binding="{Binding IsModified}"
                                                                         Value="True">
                                                                <Setter Property="Fill" Value="#dcdcaa"/>
                                                            </DataTrigger>
                                                            <DataTrigger Binding="{Binding IsModified}"
                                                                         Value="False">
                                                                <Setter Property="Fill" Value="#6a9955"/>
                                                            </DataTrigger>
                                                        </Style.Triggers>
                                                    </Style>
                                                </Ellipse.Style>
                                            </Ellipse>
                                            <TextBlock Text="{Binding FileName}"
                                                       Foreground="#dcdcdc" FontSize="11"
                                                       MaxWidth="180"
                                                       TextTrimming="CharacterEllipsis"/>
                                            <Button Content="✕" FontSize="10"
                                                    Foreground="#888"
                                                    Background="Transparent"
                                                    BorderThickness="0"
                                                    Padding="6,0,0,0"
                                                    Cursor="Hand"
                                                    Click="TabClose_Click"
                                                    Tag="{Binding}"/>
                                        </StackPanel>
                                    </Border>
                                </DataTemplate>
                            </ItemsControl.ItemTemplate>
                        </ItemsControl>

                        <!-- + button -->
                        <Button Content="+" FontSize="14"
                                Foreground="#888" Background="Transparent"
                                BorderThickness="0" Padding="8,6" Cursor="Hand"
                                Click="AddTab_Click"/>
                    </StackPanel>
                </ScrollViewer>
            </Grid>
        </Border>

        <!-- Row 2: Main Content Area -->
        <Grid Grid.Row="2">
            <Grid.ColumnDefinitions>
                <ColumnDefinition Width="Auto"/>
                <ColumnDefinition Width="*"/>
            </Grid.ColumnDefinitions>

            <!-- TOC Sidebar -->
            <Border Grid.Column="0" Background="#252525"
                    BorderBrush="#333" BorderThickness="0,0,1,0"
                    Width="200"
                    Visibility="{Binding IsTocVisible, Converter={StaticResource BoolToVis}}">
                <Grid>
                    <Grid.RowDefinitions>
                        <RowDefinition Height="Auto"/>
                        <RowDefinition Height="*"/>
                    </Grid.RowDefinitions>

                    <TextBlock Grid.Row="0" Text="ON THIS PAGE"
                               Foreground="#555" FontSize="10"
                               FontWeight="SemiBold" Margin="10,8,10,6"/>

                    <ScrollViewer Grid.Row="1" VerticalScrollBarVisibility="Auto">
                        <ItemsControl ItemsSource="{Binding TocHeadings}">
                            <ItemsControl.ItemTemplate>
                                <DataTemplate DataType="models:TocHeading">
                                    <Border Padding="6,4" Margin="4,1" CornerRadius="3"
                                            Cursor="Hand"
                                            MouseLeftButtonDown="TocItem_Click"
                                            Tag="{Binding}">
                                        <Border.Style>
                                            <Style TargetType="Border">
                                                <Setter Property="Background" Value="Transparent"/>
                                                <Style.Triggers>
                                                    <DataTrigger Binding="{Binding IsActive}"
                                                                 Value="True">
                                                        <Setter Property="Background" Value="#3a5a8a"/>
                                                    </DataTrigger>
                                                </Style.Triggers>
                                            </Style>
                                        </Border.Style>
                                        <TextBlock Text="{Binding Text}"
                                                   Margin="{Binding Indent}"
                                                   Foreground="#b0b0b0" FontSize="11"
                                                   TextTrimming="CharacterEllipsis"
                                                   ToolTip="{Binding Text}">
                                            <TextBlock.Style>
                                                <Style TargetType="TextBlock">
                                                    <Style.Triggers>
                                                        <DataTrigger Binding="{Binding Level}" Value="1">
                                                            <Setter Property="FontWeight" Value="Bold"/>
                                                            <Setter Property="Foreground" Value="#dcdcdc"/>
                                                        </DataTrigger>
                                                    </Style.Triggers>
                                                </Style>
                                            </TextBlock.Style>
                                        </TextBlock>
                                    </Border>
                                </DataTemplate>
                            </ItemsControl.ItemTemplate>
                        </ItemsControl>
                    </ScrollViewer>
                </Grid>
            </Border>

            <!-- TOC resize gripper -->
            <GridSplitter Grid.Column="0" Width="3"
                          Background="Transparent"
                          HorizontalAlignment="Right"
                          Visibility="{Binding IsTocVisible, Converter={StaticResource BoolToVis}}"/>

            <!-- Content Area -->
            <Grid Grid.Column="1">
                <!-- WebView2 for Markdown rendering -->
                <wv2:WebView2 x:Name="MarkdownWebView"
                              Visibility="{Binding HasTabs, Converter={StaticResource BoolToVis}}"
                              xmlns:wv2="clr-namespace:Microsoft.Web.WebView2.Wpf;assembly=Microsoft.Web.WebView2.Wpf"/>

                <!-- Landing page (no tabs open) -->
                <Border Background="#1e1e1e"
                        Visibility="{Binding HasTabs, Converter={StaticResource InverseBoolToVis}}">
                    <StackPanel VerticalAlignment="Center" HorizontalAlignment="Center"
                                Margin="0,-40,0,0">
                        <TextBlock Text="📘" FontSize="48"
                                   Foreground="#555" TextAlignment="Center"
                                   Margin="0,0,0,12"/>
                        <TextBlock Text="Markdown Viewer"
                                   FontSize="18" FontWeight="SemiBold"
                                   Foreground="#dcdcdc" TextAlignment="Center"
                                   Margin="0,0,0,4"/>
                        <TextBlock Text="Open a Markdown file to get started"
                                   FontSize="12"
                                   Foreground="#888" TextAlignment="Center"
                                   Margin="0,0,0,24"/>
                        <Button Content="📂 Open File" FontSize="13"
                                Command="{Binding OpenFileCommand}"
                                Background="#007acc" Foreground="White"
                                BorderThickness="0" Padding="14,8"
                                Cursor="Hand" HorizontalAlignment="Center">
                            <Button.Resources>
                                <Style TargetType="Border">
                                    <Setter Property="CornerRadius" Value="6"/>
                                </Style>
                            </Button.Resources>
                        </Button>
                        <TextBlock Text="Ctrl+O" FontSize="11"
                                   Foreground="#555" TextAlignment="Center"
                                   Margin="0,6,0,0"/>

                        <Border Margin="0,32,0,0" Width="320"
                                Visibility="{Binding RecentFiles.Count, Converter={StaticResource BoolToVis}}">
                            <StackPanel>
                                <TextBlock Text="RECENTLY OPENED"
                                           Foreground="#555" FontSize="10"
                                           Margin="0,0,0,8"/>
                                <ItemsControl ItemsSource="{Binding RecentFiles}"
                                              MaxHeight="200">
                                    <ItemsControl.ItemTemplate>
                                        <DataTemplate DataType="models:RecentFileEntry">
                                            <Border Padding="8,5" CornerRadius="4"
                                                    Cursor="Hand"
                                                    MouseLeftButtonDown="RecentFile_Click">
                                                <StackPanel Orientation="Horizontal">
                                                    <TextBlock Text="📄" Foreground="#555"
                                                               FontSize="12" Margin="0,0,8,0"/>
                                                    <TextBlock Text="{Binding FileName}"
                                                               Foreground="#c8c8c8"
                                                               FontSize="12"/>
                                                </StackPanel>
                                            </Border>
                                        </DataTemplate>
                                    </ItemsControl.ItemTemplate>
                                </ItemsControl>
                            </StackPanel>
                        </Border>
                    </StackPanel>
                </Border>
            </Grid>
        </Grid>

        <!-- Row 3: Status Bar -->
        <Border Grid.Row="3" Background="#007acc" Padding="4,2,10,2">
            <Grid>
                <TextBlock Grid.Column="0" Text="{Binding StatusText}"
                           Foreground="White" FontSize="10"/>
                <StackPanel Grid.Column="1" Orientation="Horizontal"
                            HorizontalAlignment="Right">
                    <TextBlock Foreground="#ccd" FontSize="10"
                               Text="{Binding ActiveTab.EncodingName, FallbackValue='UTF-8'}"/>
                    <TextBlock Text="  |  " Foreground="#99b" FontSize="10"/>
                    <TextBlock Text="Markdown" Foreground="#ccd" FontSize="10"/>
                </StackPanel>
            </Grid>
        </Border>
    </Grid>
</Window>
```

- [ ] **Step 2: Build and commit**

```bash
dotnet build && git add MainWindow.xaml && git commit -m "feat: add complete MainWindow XAML layout with toolbar, tabs, TOC sidebar, and landing page"
```

---

### Task 10: MainWindow.xaml.cs — code-behind, WebView2 init, drag & drop, event handlers

**Files:**
- Modify: `MainWindow.xaml.cs`

- [ ] **Step 1: Write complete code-behind**

```csharp
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using MarkdownViewer.Models;
using MarkdownViewer.Services;
using MarkdownViewer.ViewModels;

namespace MarkdownViewer;

public partial class MainWindow : Window
{
    private MainViewModel VM => (MainViewModel)DataContext;
    private MarkdownService? _markdownService;
    private string? _cssContent;

    public MainWindow()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        _markdownService = new MarkdownService();
        _cssContent = LoadCssResource();
        await MarkdownWebView.EnsureCoreWebView2Async();
        MarkdownWebView.CoreWebView2.WebMessageReceived += OnWebMessageReceived;

        VM.NavigateRequested += OnNavigateRequested;
        VM.TocScrollRequested += OnTocScrollRequested;

        // Handle CLI args
        var args = Environment.GetCommandLineArgs();
        if (args.Length > 1)
        {
            var filePath = args[1];
            if (File.Exists(filePath))
                VM.LoadFile(filePath);
        }
    }

    private static string LoadCssResource()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = "MarkdownViewer.Resources.markdown-styles.css";
        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream == null) return string.Empty;
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    private void OnNavigateRequested(object? sender, TabItem? tab)
    {
        if (tab?.HtmlContent == null || _cssContent == null)
        {
            MarkdownWebView.Visibility = Visibility.Collapsed;
            return;
        }

        MarkdownWebView.Visibility = Visibility.Visible;
        var fullHtml = _markdownService!.BuildFullHtml(tab.HtmlContent, _cssContent);
        MarkdownWebView.CoreWebView2.NavigateToString(fullHtml);
    }

    private async void OnTocScrollRequested(object? sender, TocHeading heading)
    {
        if (MarkdownWebView.CoreWebView2 == null) return;
        await MarkdownWebView.CoreWebView2.ExecuteScriptAsync(
            $"document.getElementById('{heading.Id}').scrollIntoView({{behavior:'smooth'}});");
    }

    private void OnWebMessageReceived(object? sender, Microsoft.Web.WebView2.Core.CoreWebView2WebMessageReceivedEventArgs e)
    {
        try
        {
            var json = System.Text.Json.JsonDocument.Parse(e.WebMessageAsJson);
            var root = json.RootElement;
            if (root.GetProperty("type").GetString() == "toc-scroll")
            {
                var headingId = root.GetProperty("headingId").GetString();
                if (headingId != null)
                {
                    Dispatcher.Invoke(() => VM.OnWebMessageReceived(headingId));
                }
            }
        }
        catch { /* ignore malformed messages */ }
    }

    // ── Tab Bar Handlers ─────────────────────────────

    private void Tab_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is Border border && border.Tag is TabItem tab)
            VM.SelectTabCommand.Execute(tab);
    }

    private void Tab_MiddleClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is Border border && border.Tag is TabItem tab)
            VM.CloseTabCommand.Execute(tab);
    }

    private void TabClose_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is TabItem tab)
            VM.CloseTabCommand.Execute(tab);
    }

    private void AddTab_Click(object sender, RoutedEventArgs e)
    {
        VM.OpenFileCommand.Execute(null);
    }

    // ── TOC Handler ──────────────────────────────────

    private void TocItem_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is Border border && border.Tag is TocHeading heading)
            VM.OnTocHeadingClicked(heading);
    }

    // ── Recent Files Handler ─────────────────────────

    private void RecentFile_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is Border border && border.DataContext is RecentFileEntry entry)
        {
            RecentPopup.IsOpen = false;
            VM.OpenRecentFileCommand.Execute(entry);
        }
    }

    // ── Drag & Drop ──────────────────────────────────

    protected override void OnDragOver(DragEventArgs e)
    {
        base.OnDragOver(e);
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
            e.Effects = DragDropEffects.Copy;
        else
            e.Effects = DragDropEffects.None;
        e.Handled = true;
    }

    protected override void OnDrop(DragEventArgs e)
    {
        base.OnDrop(e);
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            var files = (string[])e.Data.GetData(DataFormats.FileDrop);
            VM.OnFileDropped(files);
        }
    }

    // ── Keyboard Handlers ────────────────────────────

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        // Ctrl+Tab / Ctrl+Shift+Tab (check Shift combo first)
        if (e.KeyboardDevice.Modifiers == (ModifierKeys.Control | ModifierKeys.Shift) && e.Key == Key.Tab)
        {
            VM.SelectPreviousTab();
            e.Handled = true;
        }
        else if (e.KeyboardDevice.Modifiers == ModifierKeys.Control && e.Key == Key.Tab)
        {
            VM.SelectNextTab();
            e.Handled = true;
        }

        // Ctrl+W
        if (e.KeyboardDevice.Modifiers == ModifierKeys.Control && e.Key == Key.W)
        {
            VM.CloseCurrentTab();
            e.Handled = true;
        }
    }
}
```

- [ ] **Step 2: Remove unused usings from MainWindow.xaml.cs**

Remove all the `using` statements that were in the original template (System.Text, System.Windows.Controls, etc.) — the code above includes only the needed ones plus System.Windows and System.Windows.Input which are required.

- [ ] **Step 3: Build and fix any compilation errors**

```bash
dotnet build
```

If there are XAML binding path warnings, they are non-fatal. Only fix compilation errors.

- [ ] **Step 4: Commit**

```bash
git add MainWindow.xaml.cs && git commit -m "feat: add MainWindow code-behind with WebView2 init, drag-drop, and event handlers"
```

---

### Task 11: App.xaml — global styles and WebView2 namespace

**Files:**
- Modify: `App.xaml`

- [ ] **Step 1: Update App.xaml with minimal global styles**

```xml
<Application x:Class="MarkdownViewer.App"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             StartupUri="MainWindow.xaml">
    <Application.Resources>
        <Style TargetType="Button">
            <Setter Property="Cursor" Value="Hand"/>
        </Style>
    </Application.Resources>
</Application>
```

- [ ] **Step 2: Build and commit**

```bash
dotnet build && git add App.xaml && git commit -m "style: add global Button cursor style in App.xaml"
```

---

### Task 12: Wire up Recent button popup and build verification

**Files:**
- Modify: `MainWindow.xaml.cs`

- [ ] **Step 1: Add Recent button click handler in the code-behind**

Add this method to `MainWindow.xaml.cs`:

```csharp
private void RecentButton_Click(object sender, RoutedEventArgs e)
{
    RecentPopup.IsOpen = !RecentPopup.IsOpen;
}
```

- [ ] **Step 2: Update the Recent button in MainWindow.xaml to use the handler**

Find this line:
```xml
<Button x:Name="RecentButton"
```

Add `Click="RecentButton_Click"` to the button attributes:
```xml
<Button x:Name="RecentButton" Click="RecentButton_Click"
```

- [ ] **Step 3: Build and verify**

```bash
dotnet build
```

Expected: Build succeeds with 0 errors.

- [ ] **Step 4: Commit**

```bash
git add MainWindow.xaml MainWindow.xaml.cs && git commit -m "fix: wire up Recent button popup toggle"
```

---

### Task 13: Ensure tab switching re-navigates WebView2

**Files:**
- Modify: `ViewModels/MainViewModel.cs`

- [ ] **Step 1: In the `SelectTab` method, ensure NavigateRequested fires**

Update the `SelectTab` method in MainViewModel:

```csharp
private void SelectTab(TabItem? tab)
{
    if (tab != null && tab != ActiveTab)
        ActiveTab = tab;
}
```

The `ActiveTab` setter already fires `NavigateRequested` via `SetProperty`, so clicking a different tab will trigger re-navigation. Build to confirm it compiles.

```bash
dotnet build
```

- [ ] **Step 2: Commit**

```bash
git add ViewModels/MainViewModel.cs && git commit -m "fix: ensure tab switching triggers WebView2 re-navigation"
```

---

### Task 14: Final build and smoke test

**Files:**
- None (verification only)

- [ ] **Step 1: Clean build**

```bash
dotnet clean && dotnet build
```

Expected: 0 errors, 0 warnings.

- [ ] **Step 2: Verify all files are in place**

```bash
ls -la Models/ Services/ ViewModels/ Converters/ Resources/ MainWindow.xaml MainWindow.xaml.cs App.xaml
```

Expected: All 12+ files exist.

- [ ] **Step 3: Run the application**

```bash
dotnet run
```

Verify:
- [ ] Window opens at 1100×700, dark themed
- [ ] Landing page shows "Markdown Viewer" with Open File button
- [ ] Click `📂 Open` → file dialog opens
- [ ] Open a .md file → renders in WebView2, TOC populates
- [ ] Click TOC items → scrolls to heading
- [ ] Open multiple files → tabs appear, switching works
- [ ] Close tab → tab removed, next tab becomes active
- [ ] Toggle TOC sidebar via button
- [ ] Recent files button shows dropdown
- [ ] Drag & drop .md file onto window opens it
- [ ] Status bar shows word count and read time

- [ ] **Step 4: Commit if any final fixes were made**

```bash
git status
git add -A
git commit -m "chore: final adjustments after smoke test"
```
