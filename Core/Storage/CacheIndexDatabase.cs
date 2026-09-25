using Microsoft.Data.Sqlite;
using System.IO;

namespace Swifter.Core.Storage;

public sealed class CacheIndexDatabase : IDisposable
{
    private readonly SqliteConnection _conn;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public CacheIndexDatabase()
    {
        var dbPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Swifter", "cache_index.db");
        Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);
        _conn = new SqliteConnection($"Data Source={dbPath}");
        _conn.Open();
        Initialize();
    }

    private void Initialize()
    {
        using var cmd = _conn.CreateCommand();
        cmd.CommandText = """
            CREATE TABLE IF NOT EXISTS cache_entries (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                url TEXT NOT NULL UNIQUE,
                content_type TEXT DEFAULT '',
                size_bytes INTEGER NOT NULL DEFAULT 0,
                last_accessed TEXT NOT NULL,
                created_at TEXT NOT NULL
            );
            CREATE INDEX IF NOT EXISTS idx_cache_url ON cache_entries(url);
            CREATE INDEX IF NOT EXISTS idx_cache_accessed ON cache_entries(last_accessed);
            """;
        cmd.ExecuteNonQuery();
    }

    public async Task TrackAsync(string url, string contentType, long sizeBytes)
    {
        await _lock.WaitAsync();
        try
        {
            using var cmd = _conn.CreateCommand();
            var now = DateTime.UtcNow.ToString("o");
            cmd.CommandText = """
                INSERT INTO cache_entries (url, content_type, size_bytes, last_accessed, created_at)
                VALUES (@url, @type, @size, @now, @now)
                ON CONFLICT(url) DO UPDATE SET
                    size_bytes = @size, last_accessed = @now, content_type = @type;
                """;
            cmd.Parameters.AddWithValue("@url", url);
            cmd.Parameters.AddWithValue("@type", contentType);
            cmd.Parameters.AddWithValue("@size", sizeBytes);
            cmd.Parameters.AddWithValue("@now", now);
            await cmd.ExecuteNonQueryAsync();
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<long> GetTotalSizeAsync()
    {
        await _lock.WaitAsync();
        try
        {
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = "SELECT COALESCE(SUM(size_bytes), 0) FROM cache_entries;";
            return (long)(await cmd.ExecuteScalarAsync() ?? 0L);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<int> EvictOlderThanAsync(DateTime cutoff)
    {
        await _lock.WaitAsync();
        try
        {
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = "DELETE FROM cache_entries WHERE last_accessed < @cutoff;";
            cmd.Parameters.AddWithValue("@cutoff", cutoff.ToString("o"));
            return await cmd.ExecuteNonQueryAsync();
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task ClearAllAsync()
    {
        await _lock.WaitAsync();
        try
        {
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = "DELETE FROM cache_entries;";
            await cmd.ExecuteNonQueryAsync();
        }
        finally
        {
            _lock.Release();
        }
    }

    public void Dispose()
    {
        _lock.Dispose();
        _conn.Dispose();
    }
}