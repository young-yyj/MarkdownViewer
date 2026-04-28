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
