using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using System.IO;

namespace Swifter.Core.Network;

public sealed class ShieldEngine
{
    private static ShieldEngine? _instance;
    private readonly HashSet<string> _trackerDomains = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _adPatterns = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, int> _blockedStats = new();
    private readonly object _lock = new();
    private bool _initialized;

    public static ShieldEngine Instance => _instance ??= new ShieldEngine();

    public event EventHandler<BlockEventArgs>? ResourceBlocked;

    public bool BlockTrackers { get; set; } = true;
    public bool BlockAds { get; set; } = true;
    public bool BlockPopups { get; set; } = true;
    public int TotalBlocked => _blockedStats.Values.Sum();

    private ShieldEngine()
    {
    }

    public void Initialize()
    {
        if (_initialized) return;
        _initialized = true;
        LoadTrackerDomains();
        LoadAdPatterns();
    }

    private void LoadTrackerDomains()
    {
        var trackers = new[]
        {
            "google-analytics.com", "googletagmanager.com", "googlesyndication.com",
            "doubleclick.net", "facebook.com/tr", "connect.facebook.net",
            "analytics.twitter.com", "ads.linkedin.com", "bat.bing.com",
            "adnxs.com", "adsrvr.org", "demdex.net", "bluekai.com",
            "adservice.google.com", "pagead2.googlesyndication.com",
            "tpc.googlesyndication.com", "stats.g.doubleclick.net",
            "pixel.facebook.com", "graph.facebook.com",
            "ads.tiktok.com", "analytics.tiktok.com",
            "ads.yahoo.com", "advertising.com", "outbrain.com",
            "taboola.com", "criteo.com", "rubiconproject.com",
            "pubmatic.com", "openx.net", "casalemedia.com",
            "scorecardresearch.com", "quantserve.com", "moatads.com",
            "adsafeprotected.com", "serving-sys.com", "adform.net",
            "mathtag.com", "sharethrough.com", "spotxchange.com",
            "turn.com", "everesttech.net", "bidswitch.net",
            "mookie1.com", "eyeota.net", "crwdcntrl.net",
            "tapad.com", "adsymptotic.com", "agkn.com"
        };
        lock (_lock)
        {
            foreach (var t in trackers) _trackerDomains.Add(t);
        }
    }

    private void LoadAdPatterns()
    {
        var patterns = new[]
        {
            @"/ad[s]?/", @"/banner[s]?/", @"/popup[s]?/",
            @"/advert/", @"/sponsor/", @"/promo/",
            @"ad[_-]?slot", @"ad[_-]?unit", @"ad[_-]?frame",
            @"doubleclick", @"googlesyndication", @"adsense",
            @"advertisement", @"sponsor[_-]?link"
        };
        lock (_lock)
        {
            foreach (var p in patterns) _adPatterns.Add(p);
        }
    }

    public bool ShouldBlock(string url)
    {
        if (!BlockTrackers && !BlockAds) return false;

        try
        {
            var uri = new Uri(url);
            var host = uri.Host.ToLowerInvariant();

            if (BlockTrackers)
            {
                lock (_lock)
                {
                    if (_trackerDomains.Any(d => host.EndsWith(d)))
                    {
                        RecordBlock(url, "tracker");
                        return true;
                    }
                }
            }

            if (BlockAds)
            {
                var path = uri.AbsolutePath.ToLowerInvariant();
                lock (_lock)
                {
                    if (_adPatterns.Any(p => Regex.IsMatch(path, p, RegexOptions.IgnoreCase)))
                    {
                        RecordBlock(url, "ad");
                        return true;
                    }
                }
            }
        }
        catch
        {
        }
        return false;
    }

    public string GenerateContentBlockerScript()
    {
        return """
            (function() {
                var blocked = ['google-analytics', 'doubleclick', 'facebook.com/tr', 'googletagmanager'];
                var observer = new MutationObserver(function(mutations) {
                    mutations.forEach(function(m) {
                        m.addedNodes.forEach(function(node) {
                            if (node.tagName === 'SCRIPT' && node.src) {
                                blocked.forEach(function(b) {
                                    if (node.src.indexOf(b) !== -1) {
                                        node.remove();
                                    }
                                });
                            }
                            if (node.tagName === 'IFRAME' && node.src) {
                                blocked.forEach(function(b) {
                                    if (node.src.indexOf(b) !== -1) {
                                        node.remove();
                                    }
                                });
                            }
                        });
                    });
                });
                observer.observe(document.documentElement, { childList: true, subtree: true });
                var origOpen = XMLHttpRequest.prototype.open;
                XMLHttpRequest.prototype.open = function(method, url) {
                    var shouldBlock = false;
                    blocked.forEach(function(b) {
                        if (url.indexOf(b) !== -1) shouldBlock = true;
                    });
                    if (shouldBlock) return;
                    return origOpen.apply(this, arguments);
                };
                var origFetch = window.fetch;
                window.fetch = function(url, opts) {
                    var shouldBlock = false;
                    if (typeof url === 'string') {
                        blocked.forEach(function(b) {
                            if (url.indexOf(b) !== -1) shouldBlock = true;
                        });
                    }
                    if (shouldBlock) return Promise.reject(new Error('Blocked'));
                    return origFetch.apply(this, arguments);
                };
            })();
            """;
    }

    private void RecordBlock(string url, string category)
    {
        _blockedStats.AddOrUpdate(category, 1, (_, v) => v + 1);
        ResourceBlocked?.Invoke(this, new BlockEventArgs(url, category));
    }

    public Dictionary<string, int> GetStats()
    {
        return new Dictionary<string, int>(_blockedStats);
    }

    public void AddTrackerDomain(string domain)
    {
        lock (_lock) _trackerDomains.Add(domain);
    }

    public void AddCustomBlockPattern(string pattern)
    {
        lock (_lock) _adPatterns.Add(pattern);
    }

    public void ClearStats()
    {
        _blockedStats.Clear();
    }
}

public sealed class BlockEventArgs : EventArgs
{
    public string Url { get; }
    public string Category { get; }

    public BlockEventArgs(string url, string category)
    {
        Url = url;
        Category = category;
    }
}