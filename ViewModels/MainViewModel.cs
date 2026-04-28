using System.Collections.ObjectModel;
using System.IO;
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
        ToggleThemeCommand = new RelayCommand(ToggleTheme);

        _isDarkMode = LoadInitialTheme();
        LoadRecentFiles();
    }

    private static bool LoadInitialTheme()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var path = Path.Combine(appData, "MarkdownViewer", "settings.json");
        if (File.Exists(path))
        {
            try
            {
                var json = File.ReadAllText(path);
                var doc = System.Text.Json.JsonDocument.Parse(json);
                return doc.RootElement.GetProperty("theme").GetString() == "dark";
            }
            catch { }
        }
        // fallback: detect Windows system theme
        try
        {
            using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            if (key?.GetValue("AppsUseLightTheme") is int intVal)
                return intVal == 0;
        }
        catch { }
        return true;
    }

    public ObservableCollection<TabItem> Tabs { get; } = new();
    public ObservableCollection<TocHeading> TocHeadings { get; } = new();
    public ObservableCollection<RecentFileEntry> RecentFiles { get; } = new();

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

    public RelayCommand OpenFileCommand { get; }
    public RelayCommand<TabItem> CloseTabCommand { get; }
    public RelayCommand<TabItem> SelectTabCommand { get; }
    public RelayCommand ToggleTocCommand { get; }
    public RelayCommand<RecentFileEntry> OpenRecentFileCommand { get; }
    public RelayCommand ClearRecentFilesCommand { get; }
    public RelayCommand ToggleThemeCommand { get; }

    public event EventHandler<TabItem?>? NavigateRequested;
    public event EventHandler<TocHeading>? TocScrollRequested;
    public event EventHandler? ThemeChanged;

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
        if (tab != null && tab != ActiveTab)
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

    private void ToggleToc()
    {
        IsTocVisible = !IsTocVisible;
    }

    private void ToggleTheme()
    {
        IsDarkMode = !IsDarkMode;
        OnPropertyChanged(nameof(ThemeIcon));
        OnPropertyChanged(nameof(ThemeTooltip));
        ((App)Application.Current).SwitchTheme(IsDarkMode);
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
