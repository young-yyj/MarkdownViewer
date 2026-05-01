# MarkdownViewer

A desktop Markdown preview tool built with WPF (.NET 8) and WebView2.

## Features

- **Multi-tab browsing** — open multiple Markdown files in parallel
- **Dark / Light theme** — auto-detects Windows system theme, manual toggle supported
- **TOC sidebar** — auto-generated from headings, scroll-synced with content
- **Ctrl+Wheel zoom** — zoom from 30% to 300%, persists across tab switches
- **Recent files** — remembers up to 20 recently opened files
- **Drag & drop** — drag `.md` files directly onto the window
- **Code block copy** — one-click copy button on every code block
- **Read time estimate** — displayed in the status bar
- **File size warning** — prompts for files over 5MB

## Tech Stack

- [.NET 8](https://dotnet.microsoft.com/) + WPF
- [Microsoft WebView2](https://learn.microsoft.com/en-us/microsoft-edge/webview2/) — Chromium-based rendering
- [Markdig](https://github.com/xoofx/markdig) — Markdown parsing with advanced extensions

## Getting Started

```bash
# Build
dotnet build

# Publish (self-contained single exe, win-x64)
dotnet publish -c Release
```

The published output is at `publish/win-x64/MarkdownViewer.exe`.

## Keyboard Shortcuts

| Keys | Action |
|---|---|
| `Ctrl + O` | Open file |
| `Ctrl + W` | Close current tab |
| `Ctrl + Tab` | Next tab |
| `Ctrl + Shift + Tab` | Previous tab |
| `Ctrl + T` | Toggle TOC sidebar |
| `Ctrl + Wheel` | Zoom in/out |
| `Ctrl + 0` | Reset zoom to 100% |

## Project Structure

```
MarkdownViewer/
├── App.xaml.cs           — Application entry, theme initialization
├── MainWindow.xaml/.cs   — Main window UI and event handling
├── Converters/           — WPF value converters
├── Models/               — Data models (TabItem, TocHeading, RecentFileEntry)
├── Services/             — MarkdownService (parsing), RecentFilesService
├── ViewModels/           — MainViewModel, RelayCommand, ObservableObject
├── Resources/            — Embedded CSS (dark/light), logo.ico
└── Themes/               — WPF resource dictionaries (Dark.xaml, Light.xaml)
```

## License

MIT
