using Microsoft.Web.WebView2.Core;

namespace Swifter.Core.Protocols;

public sealed class SwifterSchemeHandler
{
    private static SwifterSchemeHandler? _instance;

    public static SwifterSchemeHandler Instance => _instance ??= new SwifterSchemeHandler();

    private readonly Dictionary<string, Func<string>> _pages = new();

    public SwifterSchemeHandler()
    {
        _instance = this;
        RegisterPages();
    }

    private void RegisterPages()
    {
        _pages["newtab"] = () => InternalPages.NewTabPage.GetHtml();
        _pages["downloads"] = () => InternalPages.DownloadsPage.GetHtml();
        _pages["history"] = () => InternalPages.HistoryPage.GetHtml();
        _pages["bookmarks"] = () => InternalPages.BookmarksPage.GetHtml();
        _pages["shields"] = () => InternalPages.ShieldsPage.GetHtml();
        _pages["vault"] = () => InternalPages.VaultPage.GetHtml();
        _pages["extensions"] = () => InternalPages.ExtensionsPage.GetHtml();
        _pages["workspaces"] = () => InternalPages.WorkspacesPage.GetHtml();
        _pages["notes"] = () => InternalPages.NotesPage.GetHtml();
        _pages["rss"] = () => InternalPages.RSSPage.GetHtml();
        _pages["torrent"] = () => InternalPages.TorrentPage.GetHtml();
        _pages["inspector"] = () => InternalPages.InspectorPage.GetHtml();
        _pages["customization"] = () => InternalPages.CustomizationPage.GetHtml();
        _pages["settings"] = () => InternalPages.SettingsPage.GetHtml();
    }

    public void RegisterProtocol()
    {
    }

    public string? HandleRequest(string url)
    {
        try
        {
            var uri = new Uri(url);
            if (uri.Scheme != "swifter") return null;
            var page = uri.Host.ToLowerInvariant();
            if (string.IsNullOrEmpty(page)) page = "newtab";
            if (_pages.TryGetValue(page, out var generator))
            {
                return generator();
            }
            return GetNotFoundPage(page);
        }
        catch
        {
            return GetErrorPage();
        }
    }

    private static string GetNotFoundPage(string page)
    {
        return $"""
            <html><head><style>
            body{{font-family:'Segoe UI',sans-serif;background:#1a1a2e;color:#e0e0e0;display:flex;align-items:center;justify-content:center;min-height:100vh;margin:0;}}
            .container{{text-align:center;}}
            h1{{font-size:72px;color:#333;margin:0;}}
            h2{{color:#888;font-weight:400;margin:8px 0 24px;}}
            a{{color:#0078d4;text-decoration:none;}}
            </style></head><body><div class="container">
            <h1>404</h1>
            <h2>Page not found: swifter://{page}</h2>
            <a href="swifter://newtab">← Back to New Tab</a>
            </div></body></html>
            """;
    }

    private static string GetErrorPage()
    {
        return """
            <html><head><style>
            body{font-family:'Segoe UI',sans-serif;background:#1a1a2e;color:#e0e0e0;display:flex;align-items:center;justify-content:center;min-height:100vh;margin:0;}
            .container{text-align:center;}
            h1{font-size:48px;color:#c23;}
            </style></head><body><div class="container">
            <h1>⚠️ Error</h1>
            <p>Something went wrong loading this internal page.</p>
            <a href="swifter://newtab" style="color:#0078d4;">← Back to New Tab</a>
            </div></body></html>
            """;
    }
}