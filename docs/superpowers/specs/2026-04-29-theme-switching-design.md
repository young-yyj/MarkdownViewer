# Theme Switching — Design Spec

**Date:** 2026-04-29  
**Status:** Approved  
**Project:** MarkdownViewer (WPF, .NET 8.0)

---

## 1. Overview

Add theme switching support: Dark Dev (current) and Light White (new day mode). Default follows Windows system theme, with toolbar toggle button for manual override. Theme choice persists to disk.

---

## 2. Visual Design

### 2.1 Light Theme Palette

| Role | Dark | Light |
|---|---|---|
| Window BG | `#1e1e1e` | `#ffffff` |
| Surface (toolbar, sidebar, tabs) | `#252525` | `#f5f5f5` |
| Button BG | `#3a3a3a` | `#e8e8e8` |
| Elevated (dropdown, table header) | `#2d2d2d` | `#ffffff` |
| Border | `#333333` | `#e1e1e1` |
| Accent | `#007acc` | `#007acc` |
| TOC Active BG | `#3a5a8a` | `#cce5ff` |
| TOC Hover BG | `#333333` | `#e8e8e8` |
| Text Primary (headings) | `#dcdcdc` | `#1a1a1a` |
| Text Body | `#c8c8c8` | `#333333` |
| Text Dimmed | `#888888` | `#666666` |
| TOC Dot Active | `#6a9955` | `#22863a` |
| TOC Dot Normal | `#666666` | `#999999` |
| Code BG | `#1a1a1a` | `#f6f8fa` |
| Inline Code BG | `#3a3a3a` | `#f0f0f0` |
| Blockquote BG | `#252525` | `#f5f5f5` |

### 2.2 Toggle Button

- Toolbar, right side (before ☰ TOC button)
- Dark mode: 🌙 icon, yellow `#dcdcaa`
- Light mode: ☀️ icon, yellow `#e6a817`
- Tooltip: "Switch to Light/Dark Theme"

---

## 3. Architecture

### 3.1 ResourceDictionary Approach

Two WPF ResourceDictionary files with identical keys, swapped at runtime via `Application.Current.Resources.MergedDictionaries`.

### 3.2 Dual CSS Approach

Two CSS files embedded as resources. `MarkdownService.BuildFullHtml()` accepts a theme parameter and loads the corresponding CSS.

---

## 4. Files

| File | Action |
|---|---|
| `Themes/Dark.xaml` | **CREATE** — Dark color SolidColorBrush resources |
| `Themes/Light.xaml` | **CREATE** — Light color SolidColorBrush resources |
| `Resources/markdown-styles-dark.css` | **RENAME** from `markdown-styles.css` |
| `Resources/markdown-styles-light.css` | **CREATE** — Light theme CSS |
| `MainWindow.xaml` | **MODIFY** — Replace hardcoded colors with `{DynamicResource}` |
| `MainWindow.xaml.cs` | **MODIFY** — Theme changed handler, re-render all tabs |
| `App.xaml` | **MODIFY** — Load default theme dictionary |
| `App.xaml.cs` | **MODIFY** — Startup theme detection, theme switch method |
| `ViewModels/MainViewModel.cs` | **MODIFY** — Theme property, toggle command, load/save preference |
| `Services/MarkdownService.cs` | **MODIFY** — Accept theme CSS resource name |
| `MarkdownViewer.csproj` | **MODIFY** — Embed both CSS files |

---

## 5. Resource Keys (18)

`WindowBg`, `SurfaceBg`, `ButtonBg`, `ElevatedBg`, `BorderBrush`, `AccentBrush`, `TabActiveBorder`, `TabBg`, `TocActiveBg`, `TocHoverBg`, `TextPrimary`, `TextBody`, `TextDimmed`, `DotActive`, `DotNormal`, `TocHeaderText`, `TocItemText`, `StatusBarBg`

---

## 6. Theme Detection & Persistence

### 6.1 Startup Logic

1. Read `%AppData%/MarkdownViewer/settings.json`
2. If `theme` key exists → use stored value
3. Else → detect Windows system theme via registry `HKCU\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize\AppsUseLightTheme`
4. Load matching ResourceDictionary

### 6.2 Toggle

1. User clicks toolbar toggle button
2. `MainViewModel.IsDarkMode` flips
3. `App.SwitchTheme(isDark)` replaces `MergedDictionaries[0]`
4. `DynamicResource` bindings auto-refresh across all UI elements
5. `MainViewModel` raises `ThemeChanged` event
6. `MainWindow` responds: re-navigate all open tabs with new CSS
7. Save preference to `settings.json`

### 6.3 Persistence

```
%AppData%/MarkdownViewer/settings.json
{
  "theme": "dark" | "light"
}
```

Same directory as `recent.json`.

---

## 7. Tab Re-rendering on Theme Change

When theme changes while files are open:
1. `ThemeChanged` event fires
2. MainWindow iterates all `VM.Tabs`
3. For each tab, re-run `MarkdownService.Parse()` on stored `RawMarkdown` with new CSS
4. Update `tab.HtmlContent`
5. If tab is `ActiveTab`, navigate WebView2 to new HTML

The `RawMarkdown` field on `TabItem` (already stored) enables re-rendering without re-reading the file.

---

## 8. Toolbar Layout Update

```
[📂 Open] | [🕐 Recent ▾] | [☰ TOC]                    🌙 | word count
```

Toggle button position: between TOC button and word count, right-aligned.

---

## 9. Out of Scope

- Auto-switch on Windows theme change while app is running (requires system event listener)
- Per-file theme override
- Custom theme editor
- CSS variables / single CSS approach (dual file keeps it simple)
