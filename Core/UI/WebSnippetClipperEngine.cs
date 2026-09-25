using System.Text.Json;
using Swifter.Core.Storage;
using System.IO;

namespace Swifter.Core.UI;

public sealed class WebSnippetClipperEngine
{
    private static WebSnippetClipperEngine? _instance;

    public static WebSnippetClipperEngine Instance => _instance ??= new WebSnippetClipperEngine();

    public event EventHandler<Snippet>? SnippetSaved;

    private WebSnippetClipperEngine()
    {
    }

    public string GetSelectionClipScript()
    {
        return """
            (function() {
                var sel = window.getSelection();
                if (!sel || sel.isCollapsed) return null;
                var range = sel.getRangeAt(0);
                var container = document.createElement('div');
                container.appendChild(range.cloneContents());
                return JSON.stringify({
                    html: container.innerHTML,
                    text: sel.toString(),
                    url: location.href,
                    title: document.title,
                    rect: range.getBoundingClientRect()
                });
            })();
            """;
    }

    public string GetElementClipScript()
    {
        return """
            (function() {
                var sel = window.getSelection();
                if (!sel || sel.isCollapsed) return null;
                var node = sel.anchorNode;
                while (node && node.nodeType !== 1) node = node.parentNode;
                if (!node) return null;
                var el = node;
                var rect = el.getBoundingClientRect();
                return JSON.stringify({
                    html: el.outerHTML,
                    text: el.textContent,
                    tag: el.tagName,
                    id: el.id,
                    classes: Array.from(el.classList),
                    url: location.href,
                    title: document.title,
                    rect: { top: rect.top, left: rect.left, width: rect.width, height: rect.height }
                });
            })();
            """;
    }

    public async Task<Snippet> SaveSnippetAsync(string html, string text, string sourceUrl, string pageTitle, string tags = "")
    {
        var snippet = new Snippet
        {
            Html = html,
            Text = text,
            SourceUrl = sourceUrl,
            PageTitle = pageTitle,
            Tags = tags,
            CreatedAt = DateTime.UtcNow
        };
        var notesDb = new WebNotesDatabase();
        snippet.Id = await notesDb.CreateAsync(
            $"Clip: {pageTitle}",
            $"```html\n{html}\n```\n\n{text}",
            sourceUrl,
            tags,
            "#00BFFF"
        );
        notesDb.Dispose();
        SnippetSaved?.Invoke(this, snippet);
        return snippet;
    }

    public void ExportSnippetAsHtml(Snippet snippet, string outputPath)
    {
        var html = $$"""
            <html><head><meta charset="utf-8"><title>{{System.Net.WebUtility.HtmlEncode(snippet.PageTitle)}}</title>
            <style>body{font-family:Segoe UI,sans-serif;max-width:800px;margin:40px auto;padding:0 20px;background:#fafafa;color:#333;}
            .meta{color:#888;font-size:12px;margin-bottom:16px;border-bottom:1px solid #ddd;padding-bottom:8px;}
            .content{line-height:1.6;}</style></head><body>
            <h1>{{System.Net.WebUtility.HtmlEncode(snippet.PageTitle)}}</h1>
            <div class="meta">Clipped from <a href="{{System.Net.WebUtility.HtmlEncode(snippet.SourceUrl)}}">{{System.Net.WebUtility.HtmlEncode(snippet.SourceUrl)}}</a> on {{snippet.CreatedAt:yyyy-MM-dd HH:mm}}</div>
            <div class="content">{{html}}</div>
            </body></html>
            """;
        File.WriteAllText(outputPath, html);
    }

    public string GenerateClipsPageHtml()
    {
        return """
            <html><head><style>
            body{font-family:'Segoe UI',sans-serif;background:#1a1a2e;color:#e0e0e0;padding:32px;}
            h1{color:#00bfff;font-size:24px;}
            .clip{background:#22223a;border-radius:10px;padding:16px;margin:10px 0;}
            .clip-title{font-size:15px;font-weight:600;color:#fff;}
            .clip-meta{font-size:11px;color:#666;margin-top:4px;}
            .clip-text{font-size:13px;color:#aaa;margin-top:8px;display:-webkit-box;-webkit-line-clamp:3;-webkit-box-orient:vertical;overflow:hidden;}
            </style></head><body><h1>✂️ Web Clips</h1>
            <p>Use Shift+Click or the clip shortcut to capture web content.</p>
            </body></html>
            """;
    }
}

public sealed class Snippet
{
    public int Id { get; set; }
    public string Html { get; set; } = "";
    public string Text { get; set; } = "";
    public string SourceUrl { get; set; } = "";
    public string PageTitle { get; set; } = "";
    public string Tags { get; set; } = "";
    public DateTime CreatedAt { get; set; }
}