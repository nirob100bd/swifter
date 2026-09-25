using Swifter.Core.Storage;
using System.IO;

namespace Swifter.Core.UI;

public sealed class HistoryCleanerEngine
{
    private static HistoryCleanerEngine? _instance;

    public static HistoryCleanerEngine Instance => _instance ??= new HistoryCleanerEngine();

    public event EventHandler<CleanupResult>? CleanupCompleted;

    private HistoryCleanerEngine()
    {
    }

    public async Task<CleanupResult> CleanAsync(CleanupOptions options)
    {
        var result = new CleanupResult { StartedAt = DateTime.UtcNow };
        try
        {
            if (options.ClearHistory)
            {
                var historyDb = new HistoryDatabase();
                if (options.TimeRange == CleanupTimeRange.All)
                {
                    await historyDb.ClearAllAsync();
                }
                else
                {
                    var cutoff = GetCutoffDate(options.TimeRange);
                    await historyDb.ClearBeforeAsync(cutoff);
                }
                historyDb.Dispose();
                result.HistoryCleared = true;
            }

            if (options.ClearCookies)
            {
                Storage.CookieManager.Instance.ClearAll();
                result.CookiesCleared = true;
            }

            if (options.ClearCache)
            {
                var cacheDb = new CacheIndexDatabase();
                await cacheDb.ClearAllAsync();
                cacheDb.Dispose();
                var cachePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Swifter", "Cache");
                if (Directory.Exists(cachePath))
                {
                    foreach (var file in Directory.GetFiles(cachePath, "*", SearchOption.AllDirectories))
                    {
                        try { File.Delete(file); } catch { }
                    }
                }
                result.CacheCleared = true;
            }

            if (options.ClearFormData)
            {
                var vault = VaultDatabase.Instance;
                await vault.ClearAllAsync();
                result.FormDataCleared = true;
            }

            if (options.ClearDownloads)
            {
                var downloadDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads", "Swifter");
                if (Directory.Exists(downloadDir))
                {
                    foreach (var file in Directory.GetFiles(downloadDir))
                    {
                        try { File.Delete(file); } catch { }
                    }
                }
                result.DownloadsCleared = true;
            }

            result.Success = true;
            result.CompletedAt = DateTime.UtcNow;
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Error = ex.Message;
        }
        CleanupCompleted?.Invoke(this, result);
        return result;
    }

    private static DateTime GetCutoffDate(CleanupTimeRange range) => range switch
    {
        CleanupTimeRange.LastHour => DateTime.UtcNow.AddHours(-1),
        CleanupTimeRange.LastDay => DateTime.UtcNow.AddDays(-1),
        CleanupTimeRange.LastWeek => DateTime.UtcNow.AddDays(-7),
        CleanupTimeRange.LastMonth => DateTime.UtcNow.AddMonths(-1),
        CleanupTimeRange.LastThreeMonths => DateTime.UtcNow.AddMonths(-3),
        _ => DateTime.MinValue
    };
}

public sealed class CleanupOptions
{
    public bool ClearHistory { get; set; } = true;
    public bool ClearCookies { get; set; } = true;
    public bool ClearCache { get; set; } = true;
    public bool ClearFormData { get; set; }
    public bool ClearDownloads { get; set; }
    public CleanupTimeRange TimeRange { get; set; } = CleanupTimeRange.All;
}

public sealed class CleanupResult
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public bool HistoryCleared { get; set; }
    public bool CookiesCleared { get; set; }
    public bool CacheCleared { get; set; }
    public bool FormDataCleared { get; set; }
    public bool DownloadsCleared { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime CompletedAt { get; set; }
}

public enum CleanupTimeRange
{
    LastHour,
    LastDay,
    LastWeek,
    LastMonth,
    LastThreeMonths,
    All
}