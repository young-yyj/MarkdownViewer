using System.IO;
using System.IO.Pipes;
using System.Text.Json;
using System.Threading;
using System.Windows;
using Microsoft.Win32;

namespace MarkdownViewer;

public partial class App : Application
{
    private const string ThemeFile = "settings.json";
    private const string MutexName = "MarkdownViewer_SingleInstance";
    private const string PipeName = "MarkdownViewer_IPC_Pipe";
    private static Mutex? _mutex;

    protected override void OnStartup(StartupEventArgs e)
    {
        _mutex = new Mutex(true, MutexName, out bool createdNew);

        if (!createdNew)
        {
            ForwardToExistingInstance(e.Args);
            _mutex.Dispose();
            Shutdown();
            return;
        }

        StartPipeServer();

        base.OnStartup(e);

        var isDark = LoadThemePreference();
        if (!isDark.HasValue)
            isDark = GetWindowsSystemTheme();

        if (!isDark.Value)
        {
            var dict = Resources.MergedDictionaries[0];
            dict.Source = new Uri("Themes/Light.xaml", UriKind.Relative);
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _mutex?.Dispose();
        base.OnExit(e);
    }

    private static void ForwardToExistingInstance(string[] args)
    {
        try
        {
            using var client = new NamedPipeClientStream(".", PipeName, PipeDirection.Out);
            client.Connect(2000);
            using var writer = new StreamWriter(client);
            writer.Write(string.Join("\n", args));
            writer.Flush();
        }
        catch { }
    }

    public void StartPipeServer()
    {
        Task.Run(() =>
        {
            while (true)
            {
                try
                {
                    using var server = new NamedPipeServerStream(PipeName, PipeDirection.In);
                    server.WaitForConnection();
                    using var reader = new StreamReader(server);
                    var message = reader.ReadToEnd();

                    Dispatcher.BeginInvoke(() =>
                    {
                        if (Current.MainWindow is MainWindow mainWindow)
                        {
                            mainWindow.BringToForeground();
                            if (!string.IsNullOrEmpty(message))
                            {
                                var files = message.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                                foreach (var file in files)
                                {
                                    if (File.Exists(file))
                                        mainWindow.LoadFileFromIpc(file);
                                }
                            }
                        }
                    });
                }
                catch { }
            }
        });
    }

    public void SwitchTheme(bool isDark)
    {
        var dict = Resources.MergedDictionaries[0];
        dict.Source = new Uri(isDark ? "Themes/Dark.xaml" : "Themes/Light.xaml", UriKind.Relative);
        SaveThemePreference(isDark);
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
                return intVal == 0;
        }
        catch { }
        return true;
    }
}
