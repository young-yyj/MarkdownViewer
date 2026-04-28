using MarkdownViewer.ViewModels;

namespace MarkdownViewer.Models;

public class TocHeading : ObservableObject
{
    public string Id { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public int Level { get; set; }

    public int Indent => (Level - 1) * 12;
    public bool IsVisible => Level <= 4;

    private bool _isActive;
    public bool IsActive
    {
        get => _isActive;
        set => SetProperty(ref _isActive, value);
    }
}
