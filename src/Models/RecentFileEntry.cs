namespace MarkdownViewer.Models;

public class RecentFileEntry
{
    public string FilePath { get; set; } = string.Empty;
    public DateTime LastOpened { get; set; }

    public bool Exists => System.IO.File.Exists(FilePath);
    public string Directory => System.IO.Path.GetDirectoryName(FilePath) ?? string.Empty;
    public string FileName => System.IO.Path.GetFileName(FilePath);
}
