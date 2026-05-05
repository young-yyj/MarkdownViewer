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
        {
            var dict = Resources.MergedDictionaries[0];
            dict.Source = new Uri("Themes/Light.xaml", UriKind.Relative);
        }
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
