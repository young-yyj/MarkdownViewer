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

    public string BuildFullHtml(string bodyHtml, bool isDark)
    {
        var css = LoadCss(isDark ? "MarkdownViewer.Resources.markdown-styles-dark.css"
                                 : "MarkdownViewer.Resources.markdown-styles-light.css");
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
(function() {{
    document.querySelectorAll('.markdown-body pre').forEach(pre => {{
        const btn = document.createElement('button');
        btn.className = 'copy-btn';
        btn.textContent = '复制';
        btn.onclick = () => {{
            const code = pre.querySelector('code');
            const text = code ? code.textContent : pre.textContent;
            const ta = document.createElement('textarea');
            ta.value = text;
            ta.style.position = 'fixed';
            ta.style.opacity = '0';
            document.body.appendChild(ta);
            ta.select();
            document.execCommand('copy');
            document.body.removeChild(ta);
            btn.textContent = '已复制!';
            btn.classList.add('copied');
            setTimeout(() => {{
                btn.textContent = '复制';
                btn.classList.remove('copied');
            }}, 1500);
        }};
        pre.appendChild(btn);
    }});
}})();
(function() {{
    let zoom = 1.0;
    const minZoom = 0.3, maxZoom = 3.0, step = 0.1;

    window.addEventListener('wheel', e => {{
        if (!e.ctrlKey) return;
        e.preventDefault();
        zoom = Math.min(maxZoom, Math.max(minZoom, zoom - Math.sign(e.deltaY) * step));
        document.body.style.zoom = zoom;
        window.chrome.webview.postMessage({{type:'zoom', level: zoom}});
    }}, {{passive: false}});

    window.addEventListener('keydown', e => {{
        if (e.ctrlKey && e.key === '0') {{
            e.preventDefault();
            zoom = 1.0;
            document.body.style.zoom = zoom;
            window.chrome.webview.postMessage({{type:'zoom', level: zoom}});
        }}
    }});
}})();
</script>
</body>
</html>";
    }

    private static string InjectHeadingIds(string markdown, out List<(string id, string text, int level)> headings)
    {
        headings = new();
        var lines = markdown.Split('\n');
        bool inFencedBlock = false;
        char fenceChar = '\0';
        int fenceCount = 0;

        for (int i = 0; i < lines.Length; i++)
        {
            var trimmed = lines[i].TrimStart();

            // detect fenced code block boundaries
            if (!inFencedBlock && (trimmed.StartsWith("```") || trimmed.StartsWith("~~~")))
            {
                inFencedBlock = true;
                fenceChar = trimmed[0];
                fenceCount = trimmed.TakeWhile(c => c == fenceChar).Count();
                continue;
            }
            if (inFencedBlock)
            {
                if (trimmed.StartsWith(new string(fenceChar, fenceCount))
                    && trimmed.TrimEnd().Length == fenceCount)
                {
                    inFencedBlock = false;
                    fenceChar = '\0';
                    fenceCount = 0;
                }
                continue;
            }

            var match = Regex.Match(lines[i], @"^(#{1,6})\s+(.+)$");
            if (match.Success)
            {
                var level = match.Groups[1].Length;
                var text = match.Groups[2].Value.Trim();
                var id = Slugify(text, headings);
                headings.Add((id, text, level));
                lines[i] = $"{match.Groups[1].Value} {text} {{#{id}}}";
                continue;
            }

            // detect Setext headings (=== level 1, --- level 2)
            if (i > 0 && !string.IsNullOrWhiteSpace(lines[i - 1]))
            {
                var prev = lines[i - 1];
                if (!Regex.IsMatch(prev, @"^#{1,6}\s")) // skip if already ATX
                {
                    int setextLevel = 0;
                    if (Regex.IsMatch(lines[i], @"^={3,}$"))
                        setextLevel = 1;
                    else if (Regex.IsMatch(lines[i], @"^-{3,}$"))
                        setextLevel = 2;

                    if (setextLevel > 0)
                    {
                        var text = StripInlineMarkdown(prev);
                        if (!string.IsNullOrEmpty(text))
                        {
                            var id = Slugify(text, headings);
                            headings.Add((id, text, setextLevel));
                            lines[i] = $"{lines[i]} {{#{id}}}";
                        }
                    }
                }
            }
        }
        return string.Join('\n', lines);
    }

    private static string StripInlineMarkdown(string text)
    {
        text = Regex.Replace(text, @"!\[.*?\]\(.*?\)", "");
        text = Regex.Replace(text, @"\[([^\]]*)\]\([^)]*\)", "$1");
        text = Regex.Replace(text, @"\*{1,3}([^*]+)\*{1,3}", "$1");
        text = Regex.Replace(text, @"_{1,3}([^_]+)_{1,3}", "$1");
        text = Regex.Replace(text, @"`([^`]+)`", "$1");
        text = Regex.Replace(text, @"<[^>]+>", "");
        return text.Trim();
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

    private static string LoadCss(string resourceName)
    {
        var assembly = System.Reflection.Assembly.GetExecutingAssembly();
        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream == null) return string.Empty;
        using var reader = new System.IO.StreamReader(stream);
        return reader.ReadToEnd();
    }
}
