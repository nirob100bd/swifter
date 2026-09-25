using System.Text.RegularExpressions;
using System.Xml;
using Swifter.Core.Storage;
using System.IO;

namespace Swifter.Core.UI;

public sealed class BookmarkImporterEngine
{
    private static BookmarkImporterEngine? _instance;

    public static BookmarkImporterEngine Instance => _instance ??= new BookmarkImporterEngine();

    public event EventHandler<int>? ImportCompleted;

    private BookmarkImporterEngine()
    {
    }

    public async Task<int> ImportFromNetscapeHtmlAsync(string filePath)
    {
        if (!File.Exists(filePath)) return 0;
        var html = await File.ReadAllTextAsync(filePath);
        return await ParseNetscapeBookmarksAsync(html);
    }

    public async Task<int> ImportFromChromeAsync()
    {
        var chromePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Google", "Chrome", "User Data", "Default", "Bookmarks");
        if (!File.Exists(chromePath)) return 0;
        try
        {
            var json = await File.ReadAllTextAsync(chromePath);
            var doc = System.Text.Json.JsonDocument.Parse(json);
            int count = 0;
            if (doc.RootElement.TryGetProperty("roots", out var roots))
            {
                count = await ProcessChromeFolder(roots, "Bookmarks Bar");
            }
            ImportCompleted?.Invoke(this, count);
            return count;
        }
        catch { return 0; }
    }

    public async Task<int> ImportFromFirefoxAsync()
    {
        var ffDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Mozilla", "Firefox", "Profiles");
        if (!Directory.Exists(ffDir)) return 0;
        var places = Directory.GetFiles(ffDir, "places.sqlite", SearchOption.AllDirectories).FirstOrDefault();
        if (places == null) return 0;
        return 0;
    }

    public async Task<int> ImportFromEdgeAsync()
    {
        var edgePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Microsoft", "Edge", "User Data", "Default", "Bookmarks");
        if (!File.Exists(edgePath)) return 0;
        try
        {
            var json = await File.ReadAllTextAsync(edgePath);
            var doc = System.Text.Json.JsonDocument.Parse(json);
            int count = 0;
            if (doc.RootElement.TryGetProperty("roots", out var roots))
            {
                count = await ProcessChromeFolder(roots, "Bookmarks Bar");
            }
            ImportCompleted?.Invoke(this, count);
            return count;
        }
        catch { return 0; }
    }

    private async Task<int> ParseNetscapeBookmarksAsync(string html)
    {
        int count = 0;
        var matches = Regex.Matches(html, @"<A[^>]+HREF=""([^""]+)""[^>]*>([^<]+)</A>", RegexOptions.IgnoreCase);
        var db = new BookmarksDatabase();
        foreach (Match match in matches)
        {
            if (match.Groups.Count < 3) continue;
            var url = match.Groups[1].Value;
            var title = match.Groups[2].Value.Trim();
            if (string.IsNullOrEmpty(url) || !url.StartsWith("http")) continue;
            await db.AddAsync(url, title);
            count++;
        }
        db.Dispose();
        ImportCompleted?.Invoke(this, count);
        return count;
    }

    private async Task<int> ProcessChromeFolder(System.Text.Json.JsonElement element, string folder)
    {
        int count = 0;
        var db = new BookmarksDatabase();
        if (element.TryGetProperty("children", out var children))
        {
            foreach (var child in children.EnumerateArray())
            {
                var type = child.GetProperty("type").GetString();
                if (type == "url")
                {
                    var url = child.GetProperty("url").GetString() ?? "";
                    var name = child.GetProperty("name").GetString() ?? "";
                    if (!string.IsNullOrEmpty(url) && url.StartsWith("http"))
                    {
                        await db.AddAsync(url, name, folder);
                        count++;
                    }
                }
                else if (type == "folder")
                {
                    var name = child.GetProperty("name").GetString() ?? "Folder";
                    count += await ProcessChromeFolder(child, name);
                }
            }
        }
        db.Dispose();
        return count;
    }

    public async Task ExportToNetscapeHtmlAsync(string outputPath)
    {
        var db = new BookmarksDatabase();
        var bookmarks = await db.GetAllAsync();
        db.Dispose();
        var html = """
            <!DOCTYPE NETSCAPE-Bookmark-file-1>
            <META HTTP-EQUIV="Content-Type" CONTENT="text/html; charset=UTF-8">
            <TITLE>Bookmarks</TITLE>
            <H1>Bookmarks</H1>
            <DL><p>
            """;
        string lastFolder = "";
        foreach (var bm in bookmarks)
        {
            if (bm.Folder != lastFolder)
            {
                if (!string.IsNullOrEmpty(lastFolder)) html += "</DL><p>\n";
                html += $"<DT><H3>{System.Net.WebUtility.HtmlEncode(bm.Folder)}</H3>\n<DL><p>\n";
                lastFolder = bm.Folder;
            }
            html += $"<DT><A HREF=\"{System.Net.WebUtility.HtmlEncode(bm.Url)}\">{System.Net.WebUtility.HtmlEncode(bm.Title)}</A>\n";
        }
        if (!string.IsNullOrEmpty(lastFolder)) html += "</DL><p>\n";
        html += "</DL><p>\n";
        await File.WriteAllTextAsync(outputPath, html);
    }
}