using System.Text.RegularExpressions;
using Markdig;

namespace MarkdownViewer.Services;

public class MarkdownService
{
    private readonly MarkdownPipeline _pipeline;

    public MarkdownService()
    {
        _pipeline = new MarkdownPipelineBuilder()
            .UseAdvancedExtensions()
            .Build();
    }

    public (string html, List<(string id, string text, int level)> headings) Parse(string markdown)
    {
        var processed = InjectHeadingIds(markdown, out var headings);
        var html = Markdown.ToHtml(processed, _pipeline);
        return (html, headings);
    }

    public string BuildFullHtml(string bodyHtml, string css)
    {
        return $@"<!DOCTYPE html>
<html lang=""en"">
<head>
<meta charset=""UTF-8"">
<style>{css}</style>
</head>
<body>
<div class=""markdown-body"">
{bodyHtml}
</div>
<script>
(function() {{
    const headings = document.querySelectorAll('.markdown-body h1, .markdown-body h2, .markdown-body h3, .markdown-body h4');
    if (headings.length === 0) return;
    function updateActive() {{
        let current = headings[0];
        const top = window.scrollY + 80;
        for (const h of headings) {{
            if (h.offsetTop <= top) current = h;
        }}
        window.chrome.webview.postMessage({{type:'toc-scroll', headingId: current.id}});
    }}
    window.addEventListener('scroll', updateActive, {{passive: true}});
    updateActive();
}})();
</script>
</body>
</html>";
    }

    private static string InjectHeadingIds(string markdown, out List<(string id, string text, int level)> headings)
    {
        headings = new();
        var lines = markdown.Split('\n');
        for (int i = 0; i < lines.Length; i++)
        {
            var match = Regex.Match(lines[i], @"^(#{1,6})\s+(.+)$");
            if (!match.Success) continue;

            var level = match.Groups[1].Length;
            var text = match.Groups[2].Value.Trim();
            var id = Slugify(text, headings);
            headings.Add((id, text, level));
            lines[i] = $"{match.Groups[1].Value} {{#{id}}} {text}";
        }
        return string.Join('\n', lines);
    }

    private static string Slugify(string text, List<(string id, string, int)> existing)
    {
        var id = Regex.Replace(text.ToLowerInvariant(), @"[^a-z0-9一-鿿]+", "-").Trim('-');
        if (string.IsNullOrEmpty(id)) id = "heading";
        var original = id;
        int suffix = 1;
        while (existing.Any(h => h.id == id))
            id = $"{original}-{suffix++}";
        return id;
    }
}
