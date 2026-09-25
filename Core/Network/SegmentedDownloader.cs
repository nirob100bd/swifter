using System.IO.Pipelines;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.IO;

namespace Swifter.Core.Network;

public sealed class SegmentedDownloader : IDisposable
{
    private readonly HttpClient _httpClient;
    private int _segmentCount = 32;
    private long _minSegmentSize = 1024 * 1024;
    private int _bufferSize = 8192;
    private CancellationTokenSource? _cts;
    private bool _disposed;

    public event EventHandler<DownloadProgressEventArgs>? ProgressChanged;
    public event EventHandler<DownloadCompleteEventArgs>? Completed;
    public event EventHandler<DownloadErrorEventArgs>? Error;

    public int SegmentCount
    {
        get => _segmentCount;
        set => _segmentCount = Math.Clamp(value, 1, 64);
    }

    public bool IsDownloading { get; private set; }
    public long TotalBytes { get; private set; }
    public long DownloadedBytes { get; private set; }
    public double ProgressPercent => TotalBytes > 0 ? (double)DownloadedBytes / TotalBytes * 100 : 0;

    public SegmentedDownloader()
    {
        var handler = new SocketsHttpHandler
        {
            MaxConnectionsPerServer = 64,
            PooledConnectionLifetime = TimeSpan.FromMinutes(10),
            ConnectTimeout = TimeSpan.FromSeconds(10)
        };
        _httpClient = new HttpClient(handler);
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Swifter/1.0");
    }

    public async Task DownloadAsync(string url, string outputPath, Dictionary<string, string>? headers = null, CancellationToken cancellationToken = default)
    {
        if (IsDownloading) return;
        IsDownloading = true;
        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        DownloadedBytes = 0;

        try
        {
            using var headReq = new HttpRequestMessage(HttpMethod.Head, url);
            if (headers != null)
            {
                foreach (var h in headers) headReq.Headers.TryAddWithoutValidation(h.Key, h.Value);
            }
            using var headResp = await _httpClient.SendAsync(headReq, _cts.Token);
            headResp.EnsureSuccessStatusCode();
            TotalBytes = headResp.Content.Headers.ContentLength ?? -1;
            bool supportsRange = headResp.Headers.AcceptRanges.Contains("bytes");

            if (!supportsRange || TotalBytes <= 0 || TotalBytes < _minSegmentSize * 2)
            {
                await DownloadSingleAsync(url, outputPath, headers, _cts.Token);
            }
            else
            {
                await DownloadSegmentedAsync(url, outputPath, headers, _cts.Token);
            }

            Completed?.Invoke(this, new DownloadCompleteEventArgs(outputPath, TotalBytes));
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            Error?.Invoke(this, new DownloadErrorEventArgs(ex.Message, ex));
        }
        finally
        {
            IsDownloading = false;
            _cts?.Dispose();
            _cts = null;
        }
    }

    private async Task DownloadSingleAsync(string url, string outputPath, Dictionary<string, string>? headers, CancellationToken ct)
    {
        using var req = new HttpRequestMessage(HttpMethod.Get, url);
        if (headers != null)
        {
            foreach (var h in headers) req.Headers.TryAddWithoutValidation(h.Key, h.Value);
        }
        using var resp = await _httpClient.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);
        resp.EnsureSuccessStatusCode();
        await using var fs = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.None, _bufferSize, FileOptions.Asynchronous);
        await using var stream = await resp.Content.ReadAsStreamAsync(ct);
        var buffer = new byte[_bufferSize];
        int read;
        while ((read = await stream.ReadAsync(buffer, ct)) > 0)
        {
            await fs.WriteAsync(buffer.AsMemory(0, read), ct);
            DownloadedBytes += read;
            ProgressChanged?.Invoke(this, new DownloadProgressEventArgs(DownloadedBytes, TotalBytes, ProgressPercent));
        }
    }

    private async Task DownloadSegmentedAsync(string url, string outputPath, Dictionary<string, string>? headers, CancellationToken ct)
    {
        int segs = Math.Min(_segmentCount, (int)(TotalBytes / _minSegmentSize) + 1);
        long segSize = TotalBytes / segs;
        var tasks = new Task<byte[]>[segs];
        var progressLock = new object();

        for (int i = 0; i < segs; i++)
        {
            long start = i * segSize;
            long end = i == segs - 1 ? TotalBytes - 1 : start + segSize - 1;
            int segIndex = i;
            tasks[i] = Task.Run(async () =>
            {
                int retries = 3;
                while (retries-- > 0)
                {
                    try
                    {
                        using var req = new HttpRequestMessage(HttpMethod.Get, url);
                        req.Headers.Range = new RangeHeaderValue(start, end);
                        if (headers != null)
                        {
                            foreach (var h in headers) req.Headers.TryAddWithoutValidation(h.Key, h.Value);
                        }
                        using var resp = await _httpClient.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);
                        resp.EnsureSuccessStatusCode();
                        var data = new byte[end - start + 1];
                        int offset = 0;
                        await using var stream = await resp.Content.ReadAsStreamAsync(ct);
                        while (offset < data.Length)
                        {
                            int read = await stream.ReadAsync(data.AsMemory(offset, data.Length - offset), ct);
                            if (read == 0) break;
                            offset += read;
                            lock (progressLock)
                            {
                                DownloadedBytes += read;
                            }
                            ProgressChanged?.Invoke(this, new DownloadProgressEventArgs(DownloadedBytes, TotalBytes, ProgressPercent));
                        }
                        return data;
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                    catch
                    {
                        if (retries == 0) throw;
                        await Task.Delay(1000 * (3 - retries), ct);
                    }
                }
                return Array.Empty<byte>();
            }, ct);
        }

        var results = await Task.WhenAll(tasks);
        await using var fs = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.None, _bufferSize, FileOptions.Asynchronous);
        foreach (var seg in results)
        {
            await fs.WriteAsync(seg, ct);
        }
    }

    public void Cancel()
    {
        _cts?.Cancel();
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _cts?.Dispose();
        _httpClient.Dispose();
    }
}

public sealed class DownloadProgressEventArgs : EventArgs
{
    public long BytesDownloaded { get; }
    public long TotalBytes { get; }
    public double Percent { get; }

    public DownloadProgressEventArgs(long downloaded, long total, double percent)
    {
        BytesDownloaded = downloaded;
        TotalBytes = total;
        Percent = percent;
    }
}

public sealed class DownloadCompleteEventArgs : EventArgs
{
    public string FilePath { get; }
    public long TotalBytes { get; }

    public DownloadCompleteEventArgs(string path, long total)
    {
        FilePath = path;
        TotalBytes = total;
    }
}

public sealed class DownloadErrorEventArgs : EventArgs
{
    public string Message { get; }
    public Exception Exception { get; }

    public DownloadErrorEventArgs(string message, Exception exception)
    {
        Message = message;
        Exception = exception;
    }
}