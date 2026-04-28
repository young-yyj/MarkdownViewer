using System.IO;
using System.Windows;
using System.Windows.Input;
using Border = System.Windows.Controls.Border;
using Button = System.Windows.Controls.Button;
using MarkdownViewer.Models;
using MarkdownViewer.Services;
using MarkdownViewer.ViewModels;

namespace MarkdownViewer;

public partial class MainWindow : Window
{
    private MainViewModel VM => (MainViewModel)DataContext;
    private MarkdownService? _markdownService;
    private double _currentZoom = 1.0;

    public MainWindow()
    {
        var iconPath = System.IO.Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory, "Resources", "logo.ico");
        if (System.IO.File.Exists(iconPath))
            Icon = new System.Windows.Media.Imaging.BitmapImage(
                new Uri(iconPath, UriKind.Absolute));
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        _markdownService = new MarkdownService();

        var userData = System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "MarkdownViewer", "WebView2");
        var env = await Microsoft.Web.WebView2.Core.CoreWebView2Environment
            .CreateAsync(userDataFolder: userData);
        await MarkdownWebView.EnsureCoreWebView2Async(env);
        MarkdownWebView.CoreWebView2.WebMessageReceived += OnWebMessageReceived;

        VM.NavigateRequested += OnNavigateRequested;
        VM.TocScrollRequested += OnTocScrollRequested;
        VM.ThemeChanged += OnThemeChanged;

        var args = Environment.GetCommandLineArgs();
        if (args.Length > 1)
        {
            var filePath = args[1];
            if (File.Exists(filePath))
                VM.LoadFile(filePath);
        }
    }

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

    private void OnNavigateRequested(object? sender, TabItem? tab)
    {
        if (tab?.HtmlContent == null)
        {
            MarkdownWebView.Visibility = Visibility.Collapsed;
            return;
        }

        MarkdownWebView.Visibility = Visibility.Visible;
        var fullHtml = _markdownService!.BuildFullHtml(tab.HtmlContent, VM.IsDarkMode);
        MarkdownWebView.CoreWebView2.NavigationCompleted += ApplyStoredZoom;
        MarkdownWebView.CoreWebView2.NavigateToString(fullHtml);
    }

    private async void ApplyStoredZoom(object? sender, Microsoft.Web.WebView2.Core.CoreWebView2NavigationCompletedEventArgs e)
    {
        MarkdownWebView.CoreWebView2.NavigationCompleted -= ApplyStoredZoom;
        if (_currentZoom != 1.0)
            await MarkdownWebView.CoreWebView2.ExecuteScriptAsync(
                $"document.body.style.zoom = {_currentZoom};");
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
            var type = root.GetProperty("type").GetString();
            if (type == "toc-scroll")
            {
                var headingId = root.GetProperty("headingId").GetString();
                if (headingId != null)
                {
                    Dispatcher.Invoke(() =>
                    {
                        VM.OnWebMessageReceived(headingId);
                        ScrollTocToActive();
                    });
                }
            }
            else if (type == "zoom")
            {
                var level = root.GetProperty("level").GetDouble();
                _currentZoom = level;
            }
        }
        catch { }
    }

    private void ScrollTocToActive()
    {
        var active = VM.TocHeadings.FirstOrDefault(h => h.IsActive);
        if (active == null) return;

        var container = TocItemsControl.ItemContainerGenerator.ContainerFromItem(active);
        if (container is FrameworkElement element)
            element.BringIntoView();
    }

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

    private void TocItem_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is Border border && border.Tag is TocHeading heading)
            VM.OnTocHeadingClicked(heading);
    }

    private void RecentFile_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is Border border && border.DataContext is RecentFileEntry entry)
        {
            RecentPopup.IsOpen = false;
            VM.OpenRecentFileCommand.Execute(entry);
        }
    }

    private void RecentButton_Click(object sender, RoutedEventArgs e)
    {
        RecentPopup.IsOpen = !RecentPopup.IsOpen;
    }

    protected override void OnDragOver(DragEventArgs e)
    {
        base.OnDragOver(e);
        e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop)
            ? DragDropEffects.Copy
            : DragDropEffects.None;
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

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

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

        if (e.KeyboardDevice.Modifiers == ModifierKeys.Control && e.Key == Key.W)
        {
            VM.CloseCurrentTab();
            e.Handled = true;
        }
    }
}
