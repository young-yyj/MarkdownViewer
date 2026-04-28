# Theme Switching Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add Dark/Light theme switching with WPF ResourceDictionary hot-swap, dual CSS, system theme detection, and persistence.

**Architecture:** Two WPF ResourceDictionary files (Dark.xaml / Light.xaml) with identical 18 keys, swapped at runtime via `Application.Current.Resources.MergedDictionaries`. Two CSS files selected by theme parameter. MainViewModel tracks state, persists to JSON.

**Tech Stack:** .NET 8.0 WPF, Markdig, WebView2, Microsoft.Win32.Registry

---

### Task 1: Create Theme ResourceDictionary files (Dark.xaml + Light.xaml)

**Files:**
- Create: `Themes/Dark.xaml`
- Create: `Themes/Light.xaml`

- [ ] **Step 1: Create directory and Dark.xaml**

```bash
mkdir -p Themes
```

```xml
<!-- Themes/Dark.xaml -->
<ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
    <SolidColorBrush x:Key="WindowBg" Color="#1e1e1e"/>
    <SolidColorBrush x:Key="SurfaceBg" Color="#252525"/>
    <SolidColorBrush x:Key="ButtonBg" Color="#3a3a3a"/>
    <SolidColorBrush x:Key="ElevatedBg" Color="#2d2d2d"/>
    <SolidColorBrush x:Key="BorderBrush" Color="#333333"/>
    <SolidColorBrush x:Key="AccentBrush" Color="#007acc"/>
    <SolidColorBrush x:Key="TabActiveBorder" Color="#007acc"/>
    <SolidColorBrush x:Key="TabBg" Color="#1e1e1e"/>
    <SolidColorBrush x:Key="TocActiveBg" Color="#3a5a8a"/>
    <SolidColorBrush x:Key="TocHoverBg" Color="#333333"/>
    <SolidColorBrush x:Key="TextPrimary" Color="#dcdcdc"/>
    <SolidColorBrush x:Key="TextBody" Color="#c8c8c8"/>
    <SolidColorBrush x:Key="TextDimmed" Color="#888888"/>
    <SolidColorBrush x:Key="DotActive" Color="#6a9955"/>
    <SolidColorBrush x:Key="DotNormal" Color="#666666"/>
    <SolidColorBrush x:Key="TocHeaderText" Color="#555555"/>
    <SolidColorBrush x:Key="TocItemText" Color="#b0b0b0"/>
    <SolidColorBrush x:Key="StatusBarBg" Color="#007acc"/>
</ResourceDictionary>
```

- [ ] **Step 2: Create Light.xaml**

```xml
<!-- Themes/Light.xaml -->
<ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
    <SolidColorBrush x:Key="WindowBg" Color="#ffffff"/>
    <SolidColorBrush x:Key="SurfaceBg" Color="#f5f5f5"/>
    <SolidColorBrush x:Key="ButtonBg" Color="#e8e8e8"/>
    <SolidColorBrush x:Key="ElevatedBg" Color="#ffffff"/>
    <SolidColorBrush x:Key="BorderBrush" Color="#e1e1e1"/>
    <SolidColorBrush x:Key="AccentBrush" Color="#007acc"/>
    <SolidColorBrush x:Key="TabActiveBorder" Color="#007acc"/>
    <SolidColorBrush x:Key="TabBg" Color="#ffffff"/>
    <SolidColorBrush x:Key="TocActiveBg" Color="#cce5ff"/>
    <SolidColorBrush x:Key="TocHoverBg" Color="#e8e8e8"/>
    <SolidColorBrush x:Key="TextPrimary" Color="#1a1a1a"/>
    <SolidColorBrush x:Key="TextBody" Color="#333333"/>
    <SolidColorBrush x:Key="TextDimmed" Color="#666666"/>
    <SolidColorBrush x:Key="DotActive" Color="#22863a"/>
    <SolidColorBrush x:Key="DotNormal" Color="#999999"/>
    <SolidColorBrush x:Key="TocHeaderText" Color="#999999"/>
    <SolidColorBrush x:Key="TocItemText" Color="#555555"/>
    <SolidColorBrush x:Key="StatusBarBg" Color="#007acc"/>
</ResourceDictionary>
```

- [ ] **Step 3: Build and commit**

```bash
dotnet build && git add Themes/ && git commit -m "feat: add Dark and Light theme ResourceDictionary files"
```

---

### Task 2: Light CSS + rename dark CSS + update .csproj

**Files:**
- Rename: `Resources/markdown-styles.css` → `Resources/markdown-styles-dark.css`
- Create: `Resources/markdown-styles-light.css`
- Modify: `MarkdownViewer.csproj`

- [ ] **Step 1: Rename dark CSS**

```bash
mv Resources/markdown-styles.css Resources/markdown-styles-dark.css
```

- [ ] **Step 2: Create light CSS**

```css
/* Resources/markdown-styles-light.css */
* { box-sizing: border-box; margin: 0; padding: 0; }

body {
    font-family: 'Segoe UI', -apple-system, sans-serif;
    font-size: 14px;
    line-height: 1.7;
    color: #333333;
    background-color: #ffffff;
    padding: 20px 32px;
}

.markdown-body { max-width: 780px; margin: 0 auto; }

.markdown-body h1 {
    font-size: 26px; font-weight: 700; color: #1a1a1a;
    margin: 0 0 4px 0; padding-bottom: 6px;
    border-bottom: 1px solid #e0e0e0;
}
.markdown-body h1:not(:first-child) { margin-top: 32px; }

.markdown-body h2 {
    font-size: 20px; font-weight: 600; color: #242424;
    margin: 24px 0 8px 0; padding-bottom: 2px;
    border-bottom: 1px solid #e0e0e0;
}

.markdown-body h3 {
    font-size: 17px; font-weight: 600; color: #333333;
    margin: 20px 0 6px 0;
}

.markdown-body h4 {
    font-size: 15px; font-weight: 600; color: #444444;
    margin: 16px 0 4px 0;
}

.markdown-body h5, .markdown-body h6 {
    font-size: 14px; color: #555555; margin: 12px 0 4px 0;
}

.markdown-body p { margin: 0 0 14px 0; }

.markdown-body strong { color: #1a1a1a; }
.markdown-body em { color: #333333; }
.markdown-body del { color: #999999; }

.markdown-body a {
    color: #0366d6; text-decoration: none;
}
.markdown-body a:hover { text-decoration: underline; }

.markdown-body code {
    font-family: 'Cascadia Code', 'Consolas', monospace;
    font-size: 13px;
    background: #f0f0f0;
    padding: 2px 5px;
    border-radius: 3px;
    color: #333333;
}

.markdown-body pre {
    position: relative;
    background: #f6f8fa;
    border: 1px solid #d0d0d0;
    border-radius: 6px;
    padding: 14px 16px;
    margin: 0 0 16px 0;
    overflow-x: auto;
}

.markdown-body .copy-btn {
    position: absolute;
    top: 6px;
    right: 8px;
    padding: 3px 10px;
    font-size: 11px;
    font-family: 'Segoe UI', sans-serif;
    color: #666;
    background: #e8e8e8;
    border: 1px solid #d0d0d0;
    border-radius: 4px;
    cursor: pointer;
    opacity: 0;
    transition: opacity 0.15s;
}
.markdown-body pre:hover .copy-btn { opacity: 1; }
.markdown-body .copy-btn:hover {
    color: #333;
    background: #ddd;
}
.markdown-body .copy-btn.copied {
    color: #22863a;
    border-color: #22863a;
}

.markdown-body pre code {
    background: none;
    padding: 0;
    border-radius: 0;
    color: #333333;
    font-size: 13px;
    line-height: 1.6;
}

.markdown-body blockquote {
    border-left: 3px solid #0366d6;
    background: #f5f5f5;
    padding: 10px 14px;
    margin: 0 0 16px 0;
    border-radius: 0 4px 4px 0;
    color: #555555;
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
    border: 1px solid #e1e1e1;
    background: #f5f5f5;
    color: #1a1a1a;
    font-weight: 600;
}
.markdown-body td {
    padding: 6px 12px;
    border: 1px solid #e1e1e1;
}
.markdown-body tr:nth-child(even) td { background: #f9f9f9; }

.markdown-body hr {
    border: none;
    border-bottom: 2px solid #e0e0e0;
    margin: 20px 0;
}

.markdown-body img {
    max-width: 100%;
    border-radius: 4px;
    margin: 8px 0;
}

.markdown-body input[type="checkbox"] {
    margin-right: 6px;
    accent-color: #22863a;
}

.markdown-body .task-list-item {
    list-style: none;
    margin-left: -22px;
}
```

- [ ] **Step 3: Update .csproj to embed both CSS files**

Read `MarkdownViewer.csproj`, replace:

```xml
  <ItemGroup>
    <EmbeddedResource Include="Resources\markdown-styles.css" />
  </ItemGroup>
```

With:

```xml
  <ItemGroup>
    <EmbeddedResource Include="Resources\markdown-styles-dark.css" />
    <EmbeddedResource Include="Resources\markdown-styles-light.css" />
  </ItemGroup>
```

- [ ] **Step 4: Build and commit**

```bash
dotnet build && git add Resources/ MarkdownViewer.csproj && git commit -m "feat: add light theme CSS, rename dark CSS, update embedded resources"
```

---

### Task 3: Update App.xaml + App.xaml.cs (theme detection + switch)

**Files:**
- Modify: `App.xaml`
- Modify: `App.xaml.cs`

- [ ] **Step 1: Update App.xaml to load Dark theme by default**

```xml
<Application x:Class="MarkdownViewer.App"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             StartupUri="MainWindow.xaml">
    <Application.Resources>
        <ResourceDictionary>
            <ResourceDictionary.MergedDictionaries>
                <ResourceDictionary Source="Themes/Dark.xaml"/>
            </ResourceDictionary.MergedDictionaries>
            <Style TargetType="Button">
                <Setter Property="Cursor" Value="Hand"/>
            </Style>
        </ResourceDictionary>
    </Application.Resources>
</Application>
```

- [ ] **Step 2: Update App.xaml.cs with theme detection and switching**

```csharp
using System.IO;
using System.Text.Json;
using System.Windows;
using Microsoft.Win32;

namespace MarkdownViewer;

public partial class App : Application
{
    private const string ThemeFile = "settings.json";

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var isDark = LoadThemePreference();
        if (!isDark.HasValue)
            isDark = GetWindowsSystemTheme();

        if (!isDark.Value)
            SwitchThemeToLight();
    }

    public void SwitchTheme(bool isDark)
    {
        var dict = Resources.MergedDictionaries[0];
        dict.Source = new Uri(isDark ? "Themes/Dark.xaml" : "Themes/Light.xaml", UriKind.Relative);
        SaveThemePreference(isDark);
    }

    private void SwitchThemeToLight()
    {
        var dict = Resources.MergedDictionaries[0];
        dict.Source = new Uri("Themes/Light.xaml", UriKind.Relative);
    }

    private static bool? LoadThemePreference()
    {
        var path = GetSettingsPath();
        if (!File.Exists(path)) return null;
        try
        {
            var json = File.ReadAllText(path);
            var doc = JsonDocument.Parse(json);
            var theme = doc.RootElement.GetProperty("theme").GetString();
            return theme == "dark";
        }
        catch { return null; }
    }

    private static void SaveThemePreference(bool isDark)
    {
        var path = GetSettingsPath();
        var dir = Path.GetDirectoryName(path)!;
        Directory.CreateDirectory(dir);
        var json = JsonSerializer.Serialize(new { theme = isDark ? "dark" : "light" });
        File.WriteAllText(path, json);
    }

    private static string GetSettingsPath()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return Path.Combine(appData, "MarkdownViewer", ThemeFile);
    }

    private static bool GetWindowsSystemTheme()
    {
        try
        {
            const string keyPath = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
            using var key = Registry.CurrentUser.OpenSubKey(keyPath);
            var value = key?.GetValue("AppsUseLightTheme");
            if (value is int intVal)
                return intVal == 0; // 0 = dark, 1 = light
        }
        catch { }
        return true; // default dark if registry read fails
    }
}
```

- [ ] **Step 3: Build and commit**

```bash
dotnet build && git add App.xaml App.xaml.cs && git commit -m "feat: add theme detection, switching, and persistence in App"
```

---

### Task 4: Update MainViewModel — theme state and toggle command

**Files:**
- Modify: `ViewModels/MainViewModel.cs`

- [ ] **Step 1: Add theme properties and toggle command**

Add these fields, properties, command, and methods to MainViewModel:

Add to the constructor (after existing command initializations):
```csharp
        ToggleThemeCommand = new RelayCommand(ToggleTheme);
```

Add properties section:
```csharp
    private bool _isDarkMode = true;
    public bool IsDarkMode
    {
        get => _isDarkMode;
        set
        {
            if (SetProperty(ref _isDarkMode, value))
                ThemeChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public string ThemeIcon => IsDarkMode ? "🌙" : "☀️";
    public string ThemeTooltip => IsDarkMode ? "Switch to Light Theme" : "Switch to Dark Theme";
```

Add command:
```csharp
    public RelayCommand ToggleThemeCommand { get; }
```

Add event:
```csharp
    public event EventHandler? ThemeChanged;
```

Add method:
```csharp
    private void ToggleTheme()
    {
        IsDarkMode = !IsDarkMode;
        OnPropertyChanged(nameof(ThemeIcon));
        OnPropertyChanged(nameof(ThemeTooltip));
        ((App)Application.Current).SwitchTheme(IsDarkMode);
        SaveThemePreference();
    }

    private static void SaveThemePreference()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var dir = Path.Combine(appData, "MarkdownViewer");
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, "settings.json");
        var json = System.Text.Json.JsonSerializer.Serialize(new { theme = "dark" });
        File.WriteAllText(path, json);
    }
```

Note: The `SaveThemePreference` duplicates App's functionality but ensures the VM can save directly. The App's SaveThemePreference is called via `SwitchTheme()`.

- [ ] **Step 2: Add using directives**

Add to the top of MainViewModel.cs:
```csharp
using System.IO;
```

- [ ] **Step 3: Build and commit**

```bash
dotnet build && git add ViewModels/MainViewModel.cs && git commit -m "feat: add theme state and toggle command to MainViewModel"
```

---

### Task 5: Update MarkdownService — accept theme parameter

**Files:**
- Modify: `Services/MarkdownService.cs`

- [ ] **Step 1: Modify `BuildFullHtml` to accept a CSS resource name parameter**

Change the method signature and body:

```csharp
    public string BuildFullHtml(string bodyHtml, bool isDark)
    {
        var css = LoadCssResource(isDark ? "MarkdownViewer.Resources.markdown-styles-dark.css"
                                         : "MarkdownViewer.Resources.markdown-styles-light.css");
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
(function() {{
    document.querySelectorAll('.markdown-body pre').forEach(pre => {{
        const btn = document.createElement('button');
        btn.className = 'copy-btn';
        btn.textContent = '复制';
        btn.onclick = () => {{
            const code = pre.querySelector('code');
            const text = code ? code.textContent : pre.textContent;
            const ta = document.createElement('textarea');
            ta.value = text;
            ta.style.position = 'fixed';
            ta.style.opacity = '0';
            document.body.appendChild(ta);
            ta.select();
            document.execCommand('copy');
            document.body.removeChild(ta);
            btn.textContent = '已复制!';
            btn.classList.add('copied');
            setTimeout(() => {{
                btn.textContent = '复制';
                btn.classList.remove('copied');
            }}, 1500);
        }};
        pre.appendChild(btn);
    }});
}})();
</script>
</body>
</html>";
    }

    private static string LoadCssResource(string resourceName)
    {
        var assembly = System.Reflection.Assembly.GetExecutingAssembly();
        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream == null) return string.Empty;
        using var reader = new System.IO.StreamReader(stream);
        return reader.ReadToEnd();
    }
```

- [ ] **Step 2: Build and commit**

```bash
dotnet build && git add Services/MarkdownService.cs && git commit -m "feat: update MarkdownService to accept theme parameter for CSS selection"
```

---

### Task 6: Update MainWindow.xaml — DynamicResource + theme toggle button

**Files:**
- Modify: `MainWindow.xaml`

- [ ] **Step 1: Replace all hardcoded colors with DynamicResource references**

The key color replacements across the entire XAML:

| Hardcoded | Replace With |
|---|---|
| `Background="#1e1e1e"` | `Background="{DynamicResource WindowBg}"` |
| `Background="#252525"` | `Background="{DynamicResource SurfaceBg}"` |
| `Background="#3a3a3a"` | `Background="{DynamicResource ButtonBg}"` |
| `Background="#2d2d2d"` | `Background="{DynamicResource ElevatedBg}"` |
| `Background="#007acc"` | `Background="{DynamicResource AccentBrush}"` |
| `BorderBrush="#333"` | `BorderBrush="{DynamicResource BorderBrush}"` |
| `BorderBrush="#444"` | `BorderBrush="{DynamicResource BorderBrush}"` |
| `BorderBrush="#555"` | `BorderBrush="{DynamicResource BorderBrush}"` |
| `BorderBrush="#007acc"` | `BorderBrush="{DynamicResource AccentBrush}"` |
| `Foreground="#dcdcdc"` | `Foreground="{DynamicResource TextPrimary}"` |
| `Foreground="#c8c8c8"` | `Foreground="{DynamicResource TextBody}"` |
| `Foreground="#888"` | `Foreground="{DynamicResource TextDimmed}"` |
| `Foreground="#888888"` | `Foreground="{DynamicResource TextDimmed}"` |
| `Foreground="#666"` | `Foreground="{DynamicResource TextDimmed}"` |
| `Foreground="#555"` | `Foreground="{DynamicResource TocHeaderText}"` |
| `Foreground="#b0b0b0"` | `Foreground="{DynamicResource TocItemText}"` |
| `Fill="#6a9955"` | `Fill="{DynamicResource DotActive}"` |
| `Fill="#666"` | `Fill="{DynamicResource DotNormal}"` |
| `Value="#3a5a8a"` (TOC active) | `Value="{DynamicResource TocActiveBg}"` |
| `Value="#333333"` (TOC hover) | `Value="{DynamicResource TocHoverBg}"` |
| `Value="#6a9955"` (dot active trigger) | `Value="{DynamicResource DotActive}"` |
| `Foreground="White"` (status bar) | keep — always white on blue |

**Special handling for status bar text:** The status bar background is `StatusBarBg` but the text stays `White` for both themes since the accent blue background works with white text in both themes.

**TOC BorderBrush exceptions:**
- `BorderBrush="#333"` on TOC sidebar right border — use `BorderBrush`
- `BorderBrush="#444"` in recent popup separator — use `BorderBrush`
- `BorderBrush="#555"` on popup border — keep slightly different or use `BorderBrush`

- [ ] **Step 2: Add theme toggle button between TOC button and word count**

Find this section in the toolbar:
```xml
                <TextBlock HorizontalAlignment="Right" VerticalAlignment="Center"
                           Text="{Binding StatusText}"
                           Foreground="{DynamicResource TextDimmed}" FontSize="11"/>
```

Replace with:
```xml
                <StackPanel Orientation="Horizontal" HorizontalAlignment="Right"
                            VerticalAlignment="Center">
                    <Button Command="{Binding ToggleThemeCommand}"
                            Background="Transparent" BorderThickness="0"
                            Padding="6,4" Cursor="Hand" FontSize="13"
                            ToolTip="{Binding ThemeTooltip}">
                        <Button.Resources>
                            <Style TargetType="Border">
                                <Setter Property="CornerRadius" Value="4"/>
                            </Style>
                        </Button.Resources>
                        <TextBlock Text="{Binding ThemeIcon}"/>
                    </Button>
                    <TextBlock Text="{Binding StatusText}"
                               Foreground="{DynamicResource TextDimmed}" FontSize="11"
                               VerticalAlignment="Center" Margin="4,0,0,0"/>
                </StackPanel>
```

- [ ] **Step 3: Build and verify**

```bash
dotnet build
```

Expected: 0 errors. If there are binding path warnings for ThemeIcon/ThemeTooltip/IsDarkMode, they are non-fatal — the bindings resolve at runtime.

- [ ] **Step 4: Commit**

```bash
git add MainWindow.xaml && git commit -m "feat: replace hardcoded colors with DynamicResource, add theme toggle button"
```

---

### Task 7: Update MainWindow.xaml.cs — theme change re-rendering

**Files:**
- Modify: `MainWindow.xaml.cs`

- [ ] **Step 1: Remove CSS loading from OnLoaded and update OnNavigateRequested**

In `OnLoaded`, remove the `LoadCssResource` call and the `_cssContent` field (no longer needed since MarkdownService handles it now).

Replace `_cssContent` field usage. Remove:
```csharp
    private string? _cssContent;
```

In `OnLoaded`, remove:
```csharp
        _cssContent = LoadCssResource();
```

Remove the `LoadCssResource` method entirely.

- [ ] **Step 2: Update NavigateRequested handler**

```csharp
    private void OnNavigateRequested(object? sender, TabItem? tab)
    {
        if (tab?.HtmlContent == null)
        {
            MarkdownWebView.Visibility = Visibility.Collapsed;
            return;
        }

        MarkdownWebView.Visibility = Visibility.Visible;
        var fullHtml = _markdownService!.BuildFullHtml(tab.HtmlContent, VM.IsDarkMode);
        MarkdownWebView.CoreWebView2.NavigateToString(fullHtml);
    }
```

- [ ] **Step 3: Subscribe to ThemeChanged event to re-render all tabs**

Add to `OnLoaded` after existing event subscriptions:
```csharp
        VM.ThemeChanged += OnThemeChanged;
```

Add the handler:
```csharp
    private void OnThemeChanged(object? sender, EventArgs e)
    {
        if (_markdownService == null) return;

        foreach (var tab in VM.Tabs)
        {
            if (tab.RawMarkdown == null) continue;
            var (html, _) = _markdownService.Parse(tab.RawMarkdown);
            tab.HtmlContent = html;
        }

        if (VM.ActiveTab?.HtmlContent != null)
        {
            var fullHtml = _markdownService.BuildFullHtml(VM.ActiveTab.HtmlContent, VM.IsDarkMode);
            MarkdownWebView.CoreWebView2.NavigateToString(fullHtml);
        }
    }
```

- [ ] **Step 4: Build and commit**

```bash
dotnet build && git add MainWindow.xaml.cs && git commit -m "feat: add theme change handler to re-render all tabs with new CSS"
```

---

### Task 8: Build and smoke test

**Files:**
- None (verification only)

- [ ] **Step 1: Clean build**

```bash
dotnet clean && dotnet build
```

Expected: 0 errors, 0 warnings.

- [ ] **Step 2: Verify all files exist**

```bash
ls -la Themes/Dark.xaml Themes/Light.xaml Resources/markdown-styles-dark.css Resources/markdown-styles-light.css
```

- [ ] **Step 3: Run and verify**

```bash
dotnet run
```

Verify checklist:
- [ ] App starts in dark theme (if Windows is dark) or light theme
- [ ] Theme toggle button 🌙/☀️ visible in toolbar
- [ ] Click toggle → WPF shell switches to light theme instantly
- [ ] Content re-renders with light CSS
- [ ] Click toggle again → back to dark theme
- [ ] Open test-formats.md in both themes → all formatting correct
- [ ] Close and reopen app → theme choice persists
- [ ] Delete %AppData%/MarkdownViewer/settings.json → falls back to system theme

- [ ] **Step 4: Commit any final fixes**

```bash
git status && git add -A && git commit -m "chore: final fixes after theme switching smoke test"
```
