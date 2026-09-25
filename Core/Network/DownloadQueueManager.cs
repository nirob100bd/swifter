#pragma warning disable CS8600, CS8601, CS8602, CS8603, CS8604
using System.Collections.Concurrent;
using System.IO;

namespace Swifter.Core.Network;

public sealed class DownloadQueueManager : IDisposable
{
    private static DownloadQueueManager? _instance;
    private readonly ConcurrentQueue<DownloadTask> _queue = new();
    private readonly ConcurrentDictionary<string, DownloadTask> _active = new();
    private readonly SemaphoreSlim _semaphore;
    private readonly List<SegmentedDownloader> _downloaders = new();
    private CancellationTokenSource _cts = new();

    public static DownloadQueueManager Instance => _instance ??= new DownloadQueueManager();

    public event EventHandler<DownloadTask>? TaskStarted;
    public event EventHandler<DownloadTask>? TaskCompleted;
    public event EventHandler<DownloadTask>? TaskFailed;
    public event EventHandler<DownloadTask>? TaskProgress;

    public IReadOnlyCollection<DownloadTask> ActiveDownloads => _active.Values.ToList().AsReadOnly();
    public int MaxConcurrent { get; set; } = 5;

    private DownloadQueueManager()
    {
        _semaphore = new SemaphoreSlim(MaxConcurrent, MaxConcurrent);
    }

    public string Enqueue(string url, string fileName, string? savePath = null, Dictionary<string, string>? headers = null)
    {
        var task = new DownloadTask
        {
            Id = Guid.NewGuid().ToString("N"),
            Url = url,
            FileName = fileName,
            SavePath = savePath ?? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "Downloads", "Swifter", fileName),
            Headers = headers,
            Status = DownloadStatus.Queued,
            EnqueuedAt = DateTime.UtcNow
        };
        _queue.Enqueue(task);
        _ = ProcessQueueAsync();
        return task.Id;
    }

    private async Task ProcessQueueAsync()
    {
        await _semaphore.WaitAsync();
        try
        {
            if (!_queue.TryDequeue(out var task)) return;
            Directory.CreateDirectory(Path.GetDirectoryName(task.SavePath)!);
            _active[task.Id] = task;
            task.Status = DownloadStatus.Downloading;
            TaskStarted?.Invoke(this, task);

            var downloader = new SegmentedDownloader();
            downloader.ProgressChanged += (_, e) =>
            {
                task.BytesDownloaded = e.BytesDownloaded;
                task.TotalBytes = e.TotalBytes;
                task.ProgressPercent = e.Percent;
                TaskProgress?.Invoke(this, task);
            };
            downloader.Completed += (_, _) =>
            {
                task.Status = DownloadStatus.Completed;
                task.CompletedAt = DateTime.UtcNow;
                _active.TryRemove(task.Id, out DownloadTask _); // null-safe
                TaskCompleted?.Invoke(this, task);
            };
            downloader.Error += (_, e) =>
            {
                task.Status = DownloadStatus.Failed;
                task.ErrorMessage = e.Message;
                _active.TryRemove(task.Id, out DownloadTask _); // null-safe
                TaskFailed?.Invoke(this, task);
            };
            _downloaders.Add(downloader);
            await downloader.DownloadAsync(task.Url, task.SavePath, task.Headers, _cts.Token);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public void Cancel(string taskId)
    {
        if (_active.TryGetValue(taskId, out var task))
        {
            task.Status = DownloadStatus.Cancelled;
            _cts.Cancel();
            _cts = new CancellationTokenSource();
        }
    }

    public void CancelAll()
    {
        _cts.Cancel();
        _cts = new CancellationTokenSource();
        foreach (var task in _active.Values)
        {
            task.Status = DownloadStatus.Cancelled;
        }
        _active.Clear();
    }

    public void Dispose()
    {
        _cts.Cancel();
        _cts.Dispose();
        foreach (var d in _downloaders) d.Dispose();
        _semaphore.Dispose();
    }
}

public sealed class DownloadTask
{
    public string Id { get; set; } = "";
    public string Url { get; set; } = "";
    public string FileName { get; set; } = "";
    public string SavePath { get; set; } = "";
    public Dictionary<string, string>? Headers { get; set; }
    public DownloadStatus Status { get; set; }
    public long BytesDownloaded { get; set; }
    public long TotalBytes { get; set; }
    public double ProgressPercent { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime EnqueuedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}

public enum DownloadStatus
{
    Queued,
    Downloading,
    Completed,
    Failed,
    Cancelled
}
#pragma warning restore CS8600, CS8601, CS8602, CS8603, CS8604
