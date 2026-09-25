using System.Diagnostics;
using System.Text.RegularExpressions;
using System.IO;

namespace Swifter.Core.Network;

public sealed class TorrentStreamerEngine : IDisposable
{
    private static TorrentStreamerEngine? _instance;
    private Process? _streamerProcess;
    private bool _isStreaming;
    private TorrentInfo? _currentTorrent;

    public static TorrentStreamerEngine Instance => _instance ??= new TorrentStreamerEngine();

    public bool IsStreaming => _isStreaming;
    public TorrentInfo? CurrentTorrent => _currentTorrent;

    public event EventHandler<TorrentInfo>? TorrentLoaded;
    public event EventHandler<string>? StreamUrlReady;

    private TorrentStreamerEngine()
    {
    }

    public bool CanHandle(string url)
    {
        return url.StartsWith("magnet:", StringComparison.OrdinalIgnoreCase) ||
               url.EndsWith(".torrent", StringComparison.OrdinalIgnoreCase);
    }

    public async Task<string?> StartStreamAsync(string magnetOrTorrentUrl, int port = 8080)
    {
        if (_isStreaming) StopStream();
        try
        {
            _currentTorrent = new TorrentInfo
            {
                MagnetUri = magnetOrTorrentUrl,
                Port = port,
                StartedAt = DateTime.UtcNow
            };

            var torrentDir = Path.Combine(Path.GetTempPath(), "swifter_torrents");
            Directory.CreateDirectory(torrentDir);

            var startInfo = new ProcessStartInfo
            {
                FileName = "webtorrent",
                Arguments = $"\"{magnetOrTorrentUrl}\" --port {port} --out \"{torrentDir}\" --stream",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            _streamerProcess = Process.Start(startInfo);
            if (_streamerProcess == null) return null;

            _isStreaming = true;
            _streamerProcess.OutputDataReceived += (_, e) =>
            {
                if (e.Data != null)
                {
                    var match = Regex.Match(e.Data, @"http://[\d\.]+:\d+/[^\s]+");
                    if (match.Success)
                    {
                        _currentTorrent!.StreamUrl = match.Value;
                        StreamUrlReady?.Invoke(this, match.Value);
                    }
                }
            };
            _streamerProcess.BeginOutputReadLine();
            TorrentLoaded?.Invoke(this, _currentTorrent);

            await Task.Delay(2000);
            return _currentTorrent.StreamUrl;
        }
        catch
        {
            _isStreaming = false;
            return null;
        }
    }

    public void StopStream()
    {
        try
        {
            _streamerProcess?.Kill();
            _streamerProcess?.Dispose();
        }
        catch
        {
        }
        _streamerProcess = null;
        _isStreaming = false;
        _currentTorrent = null;
    }

    public void Dispose()
    {
        StopStream();
    }
}

public sealed class TorrentInfo
{
    public string MagnetUri { get; set; } = "";
    public string? StreamUrl { get; set; }
    public int Port { get; set; }
    public DateTime StartedAt { get; set; }
    public long TotalSize { get; set; }
    public double DownloadSpeed { get; set; }
    public double UploadSpeed { get; set; }
    public int Peers { get; set; }
}