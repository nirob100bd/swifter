using Microsoft.Data.Sqlite;
using System.IO;

namespace Swifter.Core.Storage;

public sealed class HistoryDatabase : IDisposable
{
    private readonly SqliteConnection _conn;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public HistoryDatabase()
    {
        var dbPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Swifter", "history.db");
        Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);
        _conn = new SqliteConnection($"Data Source={dbPath}");
        _conn.Open();
        Initialize();
    }

    private void Initialize()
    {
        using var cmd = _conn.CreateCommand();
        cmd.CommandText = """
            CREATE TABLE IF NOT EXISTS history (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                url TEXT NOT NULL,
                title TEXT NOT NULL DEFAULT '',
                visit_count INTEGER NOT NULL DEFAULT 1,
                last_visited TEXT NOT NULL,
                first_visited TEXT NOT NULL,
                favicon_url TEXT DEFAULT NULL
            );
            CREATE INDEX IF NOT EXISTS idx_history_url ON history(url);
            CREATE INDEX IF NOT EXISTS idx_history_last_visited ON history(last_visited DESC);
            CREATE INDEX IF NOT EXISTS idx_history_title ON history(title);
            """;
        cmd.ExecuteNonQuery();
    }

    public async Task AddVisitAsync(string url, string title, string? faviconUrl = null)
    {
        await _lock.WaitAsync();
        try
        {
            using var cmd = _conn.CreateCommand();
            var now = DateTime.UtcNow.ToString("o");
            cmd.CommandText = """
                INSERT INTO history (url, title, visit_count, last_visited, first_visited, favicon_url)
                VALUES (@url, @title, 1, @now, @now, @favicon)
                ON CONFLICT(url) DO UPDATE SET
                    title = CASE WHEN @title != '' THEN @title ELSE history.title END,
                    visit_count = history.visit_count + 1,
                    last_visited = @now,
                    favicon_url = COALESCE(@favicon, history.favicon_url);
                """;
            cmd.Parameters.AddWithValue("@url", url);
            cmd.Parameters.AddWithValue("@title", title ?? "");
            cmd.Parameters.AddWithValue("@now", now);
            cmd.Parameters.AddWithValue("@favicon", (object?)faviconUrl ?? DBNull.Value);
            await cmd.ExecuteNonQueryAsync();
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<List<HistoryEntry>> SearchAsync(string query, int limit = 50)
    {
        await _lock.WaitAsync();
        try
        {
            var results = new List<HistoryEntry>();
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = """
                SELECT url, title, visit_count, last_visited, favicon_url FROM history
                WHERE url LIKE @q OR title LIKE @q
                ORDER BY last_visited DESC LIMIT @limit;
                """;
            cmd.Parameters.AddWithValue("@q", $"%{query}%");
            cmd.Parameters.AddWithValue("@limit", limit);
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                results.Add(new HistoryEntry
                {
                    Url = reader.GetString(0),
                    Title = reader.GetString(1),
                    VisitCount = reader.GetInt32(2),
                    LastVisited = DateTime.Parse(reader.GetString(3)),
                    FaviconUrl = reader.IsDBNull(4) ? null : reader.GetString(4)
                });
            }
            return results;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<List<HistoryEntry>> GetRecentAsync(int limit = 100)
    {
        await _lock.WaitAsync();
        try
        {
            var results = new List<HistoryEntry>();
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = """
                SELECT url, title, visit_count, last_visited, favicon_url FROM history
                ORDER BY last_visited DESC LIMIT @limit;
                """;
            cmd.Parameters.AddWithValue("@limit", limit);
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                results.Add(new HistoryEntry
                {
                    Url = reader.GetString(0),
                    Title = reader.GetString(1),
                    VisitCount = reader.GetInt32(2),
                    LastVisited = DateTime.Parse(reader.GetString(3)),
                    FaviconUrl = reader.IsDBNull(4) ? null : reader.GetString(4)
                });
            }
            return results;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task DeleteUrlAsync(string url)
    {
        await _lock.WaitAsync();
        try
        {
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = "DELETE FROM history WHERE url = @url;";
            cmd.Parameters.AddWithValue("@url", url);
            await cmd.ExecuteNonQueryAsync();
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task ClearBeforeAsync(DateTime cutoff)
    {
        await _lock.WaitAsync();
        try
        {
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = "DELETE FROM history WHERE last_visited < @cutoff;";
            cmd.Parameters.AddWithValue("@cutoff", cutoff.ToString("o"));
            await cmd.ExecuteNonQueryAsync();
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
            cmd.CommandText = "DELETE FROM history;";
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

public sealed class HistoryEntry
{
    public string Url { get; set; } = "";
    public string Title { get; set; } = "";
    public int VisitCount { get; set; }
    public DateTime LastVisited { get; set; }
    public string? FaviconUrl { get; set; }
}