using System.Text.RegularExpressions;

namespace Swifter.Core.Network;

public sealed class MediaSniffer
{
    private static MediaSniffer? _instance;
    private readonly List<MediaSource> _captured = new();
    private readonly List<string> _mediaPatterns;
    private readonly object _lock = new();

    public static MediaSniffer Instance => _instance ??= new MediaSniffer();

    public event EventHandler<MediaSource>? MediaDetected;

    public IReadOnlyList<MediaSource> CapturedSources
    {
        get { lock (_lock) return _captured.ToList().AsReadOnly(); }
    }

    private MediaSniffer()
    {
        _mediaPatterns = new List<string>
        {
            @"\.m3u8(\?|$)",
            @"\.mp4(\?|$)",
            @"\.webm(\?|$)",
            @"\.mp3(\?|$)",
            @"\.m4a(\?|$)",
            @"\.ogg(\?|$)",
            @"\.wav(\?|$)",
            @"\.flac(\?|$)",
            @"\.avi(\?|$)",
            @"\.mkv(\?|$)",
            @"\.mov(\?|$)",
            @"video/mp4",
            @"audio/mpeg",
            @"application/x-mpegURL",
            @"video/webm",
            @"googlevideo\.com/videoplayback",
            @"\.ytimg\.com/",
            @"\.cdninstagram\.com/",
            @"fbcdn\.net.*\.mp4",
            @"tiktokcdn\.com/",
            @"twitch\.tv.*\.ts",
            @"vimeo\.com.*\.mp4"
        };
    }

    public MediaSource? AnalyzeRequest(string url, string contentType = "", string responseHeaders = "")
    {
        var lowerUrl = url.ToLowerInvariant();
        var lowerContentType = contentType.ToLowerInvariant();

        foreach (var pattern in _mediaPatterns)
        {
            if (Regex.IsMatch(lowerUrl, pattern, RegexOptions.IgnoreCase) ||
                Regex.IsMatch(lowerContentType, pattern, RegexOptions.IgnoreCase))
            {
                var source = new MediaSource
                {
                    Url = url,
                    ContentType = contentType,
                    DetectedAt = DateTime.UtcNow,
                    Type = DetermineMediaType(url, contentType),
                    Resolutions = ExtractResolutions(url)
                };

                lock (_lock)
                {
                    if (!_captured.Any(c => c.Url == url))
                    {
                        _captured.Add(source);
                        MediaDetected?.Invoke(this, source);
                    }
                }
                return source;
            }
        }
        return null;
    }

    public List<MediaSource> AnalyzeHtml(string html, string baseUrl)
    {
        var sources = new List<MediaSource>();
        var patterns = new[]
        {
            """<source[^>]+src=["']([^"']+)["']""",
            """<video[^>]+src=["']([^"']+)["']""",
            """<audio[^>]+src=["']([^"']+)["']""",
            """data-src=["']([^"']+\.(?:mp4|webm|m3u8|mp3|m4a)[^"']*)["']""",
            """file:\s*["']([^"']+\.m3u8[^"']*)["']""",
            """src:\s*["']([^"']+\.m3u8[^"']*)["']""",
            """["'](https?://[^"']*googlevideo\.com[^"']+)["']"""
        };

        foreach (var pattern in patterns)
        {
            var matches = Regex.Matches(html, pattern, RegexOptions.IgnoreCase);
            foreach (Match match in matches)
            {
                if (match.Groups.Count < 2) continue;
                var src = match.Groups[1].Value;
                if (!src.StartsWith("http"))
                {
                    try
                    {
                        src = new Uri(new Uri(baseUrl), src).ToString();
                    }
                    catch
                    {
                        continue;
                    }
                }
                var source = new MediaSource
                {
                    Url = src,
                    DetectedAt = DateTime.UtcNow,
                    Type = DetermineMediaType(src, ""),
                    Resolutions = ExtractResolutions(src)
                };
                sources.Add(source);
                lock (_lock)
                {
                    if (!_captured.Any(c => c.Url == src))
                    {
                        _captured.Add(source);
                        MediaDetected?.Invoke(this, source);
                    }
                }
            }
        }
        return sources;
    }

    private static MediaType DetermineMediaType(string url, string contentType)
    {
        var lower = url.ToLowerInvariant();
        if (lower.Contains(".m3u8")) return MediaType.HLSStream;
        if (lower.Contains(".mpd")) return MediaType.DashStream;
        if (lower.Contains(".mp4") || lower.Contains(".webm") || lower.Contains(".avi") || lower.Contains(".mkv") || lower.Contains(".mov"))
            return MediaType.Video;
        if (lower.Contains(".mp3") || lower.Contains(".m4a") || lower.Contains(".ogg") || lower.Contains(".flac") || lower.Contains(".wav"))
            return MediaType.Audio;
        if (contentType.Contains("video")) return MediaType.Video;
        if (contentType.Contains("audio")) return MediaType.Audio;
        return MediaType.Unknown;
    }

    private static List<string> ExtractResolutions(string url)
    {
        var resolutions = new List<string>();
        var matches = Regex.Matches(url, @"(\d{3,4})p", RegexOptions.IgnoreCase);
        foreach (Match m in matches)
        {
            resolutions.Add(m.Value);
        }
        if (resolutions.Count == 0)
        {
            resolutions.AddRange(new[] { "1080p", "720p", "480p", "360p" });
        }
        return resolutions;
    }

    public void ClearCaptured()
    {
        lock (_lock) _captured.Clear();
    }

    public MediaSource? GetHighestQuality(string url)
    {
        lock (_lock)
        {
            return _captured.FirstOrDefault(c => c.Url == url);
        }
    }
}

public sealed class MediaSource
{
    public string Url { get; set; } = "";
    public string ContentType { get; set; } = "";
    public MediaType Type { get; set; }
    public DateTime DetectedAt { get; set; }
    public List<string> Resolutions { get; set; } = new();
    public long? FileSize { get; set; }
    public TimeSpan? Duration { get; set; }
    public string Quality { get; set; } = "1080p";
}

public enum MediaType
{
    Unknown,
    Video,
    Audio,
    HLSStream,
    DashStream,
    Image
}