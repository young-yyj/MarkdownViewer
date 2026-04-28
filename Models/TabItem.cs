namespace MarkdownViewer.Models;

public class TabItem
{
    public string FilePath { get; set; } = string.Empty;

    public string FileName => string.IsNullOrEmpty(FilePath)
        ? "Untitled"
        : System.IO.Path.GetFileName(FilePath);

    public string? RawMarkdown { get; set; }
    public string? HtmlContent { get; set; }
    public List<TocHeading> TocHeadings { get; set; } = new();
    public bool IsModified { get; set; }
    public string EncodingName { get; set; } = "UTF-8";
    public int LineCount { get; set; }
    public int WordCount { get; set; }
}
