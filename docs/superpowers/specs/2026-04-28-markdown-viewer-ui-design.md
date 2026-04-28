# Markdown Viewer — UI Design Spec

**Date:** 2026-04-28  
**Status:** Approved  
**Project:** MarkdownViewer (WPF, .NET 8.0)

---

## 1. Overview

A WPF desktop application for viewing Markdown files. Target audience: personal writing and note-taking. Pure viewer (no editing). Supports multiple open files via tabs, TOC-based document navigation, and recent file management.

**Design theme:** Dark Dev — VS Code-inspired dark color palette.

---

## 2. Window Layout

### 2.1 Window Specs

| Property | Value |
|---|---|
| Default size | 1100 × 700 px |
| Minimum size | 800 × 500 px |
| Title format | `{filename}.md — MarkdownViewer` |
| Resizable | Yes |

### 2.2 Layout Structure (top to bottom)

```
┌─────────────────────────────────────────────┐
│ Title Bar:  📘 MarkdownViewer    — □ ✕      │
├─────────────────────────────────────────────┤
│ Toolbar: [📂 Open] [🕐 Recent] │ [☰ TOC] ... │
├─────────────────────────────────────────────┤
│ Tab Bar:  [● readme.md ✕] [guide.md ✕] [+] │
├────────────────┬────────────────────────────┤
│ TOC Sidebar    │  Content Area              │
│ (toggleable)   │                            │
│ 200px wide     │  Rendered Markdown         │
│ resizable      │  max-width ~780px          │
├────────────────┴────────────────────────────┤
│ Status Bar: ✅ Ready | Ln 42 | UTF-8 | MD   │
└─────────────────────────────────────────────┘
```

### 2.3 Toolbar

Left-aligned controls:
- **📂 Open** — opens Windows file dialog for `.md` files (`Ctrl+O`)
- Separator (1px vertical line)
- **🕐 Recent** — dropdown of recently opened files (max 20)
- Separator
- **☰ TOC** — toggle TOC sidebar visibility (`Ctrl+Shift+T`)

Right-aligned:
- Document stats (word count, estimated read time)

### 2.4 Tab Bar

Browser-style tabs for managing multiple open Markdown files.

| Feature | Implementation |
|---|---|
| Tab display | Filename + colored status dot + close button (✕) |
| Active tab | Highlighted background, blue bottom border (`#007acc`) |
| Status dot | Gray = unmodified, Yellow (`#dcdcaa`) = unsaved changes |
| Add tab | `+` button at end of tab row |
| Switch tab | Click to switch, `Ctrl+Tab` next, `Ctrl+Shift+Tab` previous |
| Close tab | Click ✕, middle-click, or `Ctrl+W` |
| Tab overflow | Tabs shrink to minimum width, scrollable if needed |

### 2.5 Status Bar

Blue background (`#007acc`), white text, 3px height.

Left: status text (e.g., "✅ Ready", "3 files open")
Right: cursor position (Ln X, Col Y), encoding (UTF-8), file type (Markdown)

---

## 3. TOC Sidebar

### 3.1 Behavior

- **Toggle:** Toolbar button + `Ctrl+Shift+T`. State remembered across sessions.
- **Width:** Default 200px, user-resizable by dragging right border.
- **Content:** Flat list of document headings, ordered by appearance.

### 3.2 Scroll Tracking

- Auto-highlights the heading currently nearest to the top of the viewport as user scrolls.
- Tracking runs in background even when TOC is hidden.

### 3.3 Heading Display

| Heading | Indent | Style | Default Visibility |
|---|---|---|---|
| H1 | 0px | Bold, full opacity | Visible |
| H2 | 12px | Normal, full opacity | Visible |
| H3 | 24px | Normal, dimmed | Visible |
| H4+ | 36px | Small, dimmed | Visible |
| H5/H6 | — | — | Hidden (configurable) |

### 3.4 Visual States

- **Active** (currently in view): Blue background (`#3a5a8a`)
- **Seen** (scrolled past): Normal color (`#b0b0b0`)
- **Unseen** (not yet reached): Dimmed (`#888888`)

### 3.5 Interaction

- Click heading → smooth-scroll content to that section.
- No collapse/expand — flat list only.
- Long headings truncated with ellipsis; full text in tooltip.
- No headings in document → show "No headings" placeholder text.

---

## 4. Content Area

### 4.1 Markdown Rendering

All standard Markdown elements rendered with Dark Dev styling.

### 4.2 Typography

| Element | Font | Size | Properties |
|---|---|---|---|
| Body | Segoe UI | 14px | Line height 1.6–1.8 |
| H1 | Segoe UI | 26px Bold | Bottom border separator |
| H2 | Segoe UI | 20px Bold | Bottom border separator |
| H3 | Segoe UI | 17px Semi-bold | |
| H4 | Segoe UI | 15px Semi-bold | |
| H5/H6 | Segoe UI | 14px | Bold / Italic |
| Code | Cascadia Code / Consolas | 13px | Fallback: monospace |
| Inline code | Cascadia Code / Consolas | 13px | Background `#3a3a3a`, padding 2px 5px, radius 3px |

### 4.3 Code Blocks

- Background: `#1a1a1a`, border: `1px solid #333`, radius: 6px
- Syntax highlighting: VS Code Dark+ color palette
- Left-aligned, full content width
- Horizontal scroll if line exceeds container

### 4.4 Tables

- Alternating row background (`#252525` / transparent)
- Header row: bold, background `#2d2d2d`
- Borders: `#3a3a3a`
- Full-width

### 4.5 Blockquotes

- Left border: `3px solid #569cd6`
- Background: `#252525`
- Slightly dimmed text

### 4.6 Other Elements

- **Links:** Blue (`#569cd6`), underline on hover
- **Images:** Responsive, max-width 100%, centered
- **Horizontal rules:** `2px solid #3a3a3a`
- **Task lists:** Checked = green (`#6a9955`), unchecked = gray (`#888`)
- **Strikethrough:** Dimmed text color

### 4.7 Content Width

Max content width: ~780px (centered in available space). Prevents excessively long lines for readability.

---

## 5. File Management

### 5.1 Opening Files

| Method | Behavior |
|---|---|
| `Ctrl+O` / Toolbar Open button | Windows file dialog, filter: `*.md` + all files |
| Drag & drop | Drop `.md` file(s) onto window → open in new tab(s) |
| CLI argument | `MarkdownViewer.exe path/to/file.md` → opens directly |
| Recent file click | Opens in new tab |
| Double-click recent file (landing page) | Opens in new tab |
| Tab `+` button | Opens file dialog |

### 5.2 Recent Files

- Max 20 entries, most recent first
- Stored as JSON in `%AppData%\MarkdownViewer\recent.json`
- Each entry: `{ "path": "...", "lastOpened": "ISO8601" }`
- Missing files shown grayed out with "missing" label
- Hover shows full path tooltip
- "Clear recent files" option at bottom of dropdown
- Duplicate paths moved to top (no duplicates in list)

### 5.3 Drag & Drop

- Accept `.md` and any text file
- Visual feedback: dashed border overlay pulses on drag-enter
- Drop multiple files → each opens in its own tab
- Drop folder → ignored

---

## 6. Landing Page (No File Open)

Displayed when app launches without a file:

- App icon (large, dimmed)
- "Markdown Viewer" title
- "Open a Markdown file to get started" subtitle
- Prominent **[📂 Open File]** button (`Ctrl+O`)
- Recent files list below (if any exist)

---

## 7. Context Menu (Right-Click on Content)

| Item | Shortcut |
|---|---|
| 📋 Copy | `Ctrl+C` |
| 📄 Copy as Markdown | `Ctrl+Shift+C` |
| --- | --- |
| 🔍 Find in Page | `Ctrl+F` |
| --- | --- |
| 🔗 Copy Link to Section | — |
| --- | --- |
| 🖨 Print... | — |

---

## 8. Keyboard Shortcuts

| Category | Shortcut | Action |
|---|---|---|
| File | `Ctrl+O` | Open file |
| File | `Ctrl+W` | Close current tab |
| Tabs | `Ctrl+Tab` | Next tab |
| Tabs | `Ctrl+Shift+Tab` | Previous tab |
| View | `Ctrl+Shift+T` | Toggle TOC sidebar |
| Find | `Ctrl+F` | Find in page |
| Find | `F3` | Find next |
| Find | `Shift+F3` | Find previous |

---

## 9. Color Palette (Dark Dev Theme)

| Role | Color | Usage |
|---|---|---|
| Background | `#1e1e1e` | Content area, window base |
| Surface | `#252525` | Sidebar, toolbar, cards |
| Elevated | `#2d2d2d` | Title bar, dropdown, table headers |
| Border | `#3a3a3a` | Panels, table cells |
| Accent | `#007acc` | Status bar, active tab underline |
| Selection | `#3a5a8a` | TOC active item, hover states |
| Text Primary | `#dcdcdc` | Headings, emphasis |
| Text Body | `#c8c8c8` | Paragraphs, list items |
| Text Dimmed | `#888888` | Secondary info, unseen items |
| Code Highlight | VS Code Dark+ palette | Syntax highlighting |

---

## 10. Error & Edge States

| State | Behavior |
|---|---|
| File deleted while open | Tab remains, content replaced with warning: "File not found: {path}" |
| File modified externally | Reload prompt or auto-reload (user preference) |
| Unsupported file type | Message: "Unsupported file type" |
| Empty file | Renders blank content area, TOC shows "No headings" |
| Very large file (>5MB) | Warning before opening, render with caution |

---

## 11. Out of Scope (v1)

- Editing (source or WYSIWYG)
- File browser / folder tree sidebar
- Multiple themes (only Dark Dev in v1)
- Export to PDF/HTML
- Plugins / extensions
- Auto-reload on external changes (deferred to user preference setting)
