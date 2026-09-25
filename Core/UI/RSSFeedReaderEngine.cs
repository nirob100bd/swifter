using System.Net.Http;
using System.ServiceModel.Syndication;
using System.Xml;

namespace Swifter.Core.UI;

public sealed class RSSFeedReaderEngine : IDisposable
{
    private static RSSFeedReaderEngine? _instance;
    private readonly List<RssFeed> _feeds = new();
    private readonly List<RssItem> _items = new();
    private Timer? _pollTimer;
    private readonly HttpClient _httpClient = new();

    public static RSSFeedReaderEngine Instance => _instance ??= new RSSFeedReaderEngine();

    public IReadOnlyList<RssFeed> Feeds => _feeds.AsReadOnly();
    public IReadOnlyList<RssItem> Items => _items.AsReadOnly();
    public int UnreadCount => _items.Count(i => !i.IsRead);

    public event EventHandler<RssItem>? NewItemReceived;
    public event EventHandler<int>? UnreadCountChanged;

    private RSSFeedReaderEngine()
    {
        LoadFeeds();
    }

    private void LoadFeeds()
    {
        var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Swifter", "rss_feeds.json");
        if (!File.Exists(path)) return;
        try
        {
            var json = File.ReadAllText(path);
            var feeds = System.Text.Json.JsonSerializer.Deserialize<List<RssFeed>>(json);
            if (feeds != null) _feeds.AddRange(feeds);
        }
        catch { }
    }

    public void AddFeed(string url, string name = "")
    {
        _feeds.Add(new RssFeed { Url = url, Name = name ?? url, AddedAt = DateTime.UtcNow });
        SaveFeeds();
    }

    public void RemoveFeed(string url)
    {
        _feeds.RemoveAll(f => f.Url == url);
        _items.RemoveAll(i => i.FeedUrl == url);
        SaveFeeds();
    }

    public async Task<List<RssItem>> FetchFeedAsync(string url)
    {
        try
        {
            var response = await _httpClient.GetStringAsync(url);
            using var reader = XmlReader.Create(new StringReader(response));
using System.IO;
            var feed = SyndicationFeed.Load(reader);
            if (feed == null) return new List<RssItem>();
            var items = feed.Items.Select(item => new RssItem
            {
                FeedUrl = url,
                Title = item.Title?.Text ?? "Untitled",
                Url = item.Links.FirstOrDefault()?.Uri.ToString() ?? "",
                Summary = item.Summary?.Text ?? "",
                PublishedAt = item.PublishDate.DateTime != default ? item.PublishDate.DateTime : DateTime.UtcNow,
                IsRead = false
            }).ToList();
            foreach (var item in items)
            {
                if (!_items.Any(i => i.Url == item.Url))
                {
                    _items.Add(item);
                    NewItemReceived?.Invoke(this, item);
                }
            }
            UnreadCountChanged?.Invoke(this, UnreadCount);
            return items;
        }
        catch
        {
            return new List<RssItem>();
        }
    }

    public async Task RefreshAllAsync()
    {
        foreach (var feed in _feeds)
        {
            await FetchFeedAsync(feed.Url);
        }
    }

    public void StartPolling(int intervalMinutes = 15)
    {
        _pollTimer?.Dispose();
        _pollTimer = new Timer(async _ => await RefreshAllAsync(), null, TimeSpan.Zero, TimeSpan.FromMinutes(intervalMinutes));
    }

    public void StopPolling()
    {
        _pollTimer?.Dispose();
        _pollTimer = null;
    }

    public void MarkAsRead(string itemUrl)
    {
        var item = _items.FirstOrDefault(i => i.Url == itemUrl);
        if (item != null)
        {
            item.IsRead = true;
            UnreadCountChanged?.Invoke(this, UnreadCount);
        }
    }

    public void MarkAllAsRead()
    {
        foreach (var item in _items) item.IsRead = true;
        UnreadCountChanged?.Invoke(this, 0);
    }

    private void SaveFeeds()
    {
        var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Swifter", "rss_feeds.json");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var json = System.Text.Json.JsonSerializer.Serialize(_feeds, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(path, json);
    }

    public string GenerateRssPageHtml()
    {
        var articlesHtml = string.Join("", _items.OrderByDescending(i => i.PublishedAt).Take(50).Select(i =>
        {
            var cls = i.IsRead ? "" : "unread";
            var title = System.Net.WebUtility.HtmlEncode(i.Title);
            var summary = System.Net.WebUtility.HtmlEncode(i.Summary);
            return $"""
                <div class="item {cls}" onclick="window.location.href='{i.Url}'">
                    <div class="title">{title}</div>
                    <div class="summary">{summary}</div>
                    <div class="meta">{i.PublishedAt:MMM d, yyyy} · {i.FeedUrl}</div>
                </div>
                """;
        }));
        return $$"""
            <html><head><style>
            body { font-family: 'Segoe UI', sans-serif; background: #1a1a2e; color: #e0e0e0; padding: 32px; }
            h1 { color: #0078d4; font-size: 24px; }
            .item { background: #22223a; border-radius: 10px; padding: 16px 20px; margin: 8px 0; cursor: pointer; }
            .item:hover { background: #2a2a40; }
            .item.unread { border-left: 3px solid #0078d4; }
            .title { font-size: 15px; font-weight: 600; color: #fff; }
            .summary { font-size: 13px; color: #888; margin-top: 4px; display: -webkit-box; -webkit-line-clamp: 2; -webkit-box-orient: vertical; overflow: hidden; }
            .meta { font-size: 11px; color: #555; margin-top: 6px; }
            .badge { background: #c23; color: #fff; padding: 2px 8px; border-radius: 12px; font-size: 11px; margin-left: 8px; }
            </style></head><body><h1>📰 RSS Reader <span class="badge">{{UnreadCount}} unread</span></h1>
            {{articlesHtml}}
            </body></html>
            """;
    }

    public void Dispose()
    {
        _pollTimer?.Dispose();
        _httpClient.Dispose();
    }
}

public sealed class RssFeed
{
    public string Url { get; set; } = "";
    public string Name { get; set; } = "";
    public DateTime AddedAt { get; set; }
}

public sealed class RssItem
{
    public string FeedUrl { get; set; } = "";
    public string Title { get; set; } = "";
    public string Url { get; set; } = "";
    public string Summary { get; set; } = "";
    public DateTime PublishedAt { get; set; }
    public bool IsRead { get; set; }
}