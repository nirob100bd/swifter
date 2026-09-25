using System.Text.Json;
using Microsoft.Data.Sqlite;
using System.IO;

namespace Swifter.Core.Storage;

public sealed class SessionArchiveDatabase : IDisposable
{
    private readonly SqliteConnection _conn;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public SessionArchiveDatabase()
    {
        var dbPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Swifter", "sessions.db");
        Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);
        _conn = new SqliteConnection($"Data Source={dbPath}");
        _conn.Open();
        Initialize();
    }

    private void Initialize()
    {
        using var cmd = _conn.CreateCommand();
        cmd.CommandText = """
            CREATE TABLE IF NOT EXISTS sessions (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                name TEXT NOT NULL DEFAULT '',
                tabs_json TEXT NOT NULL,
                window_state TEXT DEFAULT '',
                created_at TEXT NOT NULL,
                is_auto INTEGER NOT NULL DEFAULT 0
            );
            CREATE INDEX IF NOT EXISTS idx_sessions_created ON sessions(created_at DESC);
            """;
        cmd.ExecuteNonQuery();
    }

    public async Task<int> SaveSessionAsync(string name, List<SessionTab> tabs, string windowState = "", bool isAuto = false)
    {
        await _lock.WaitAsync();
        try
        {
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = """
                INSERT INTO sessions (name, tabs_json, window_state, created_at, is_auto)
                VALUES (@name, @tabs, @state, @now, @auto);
                """;
            cmd.Parameters.AddWithValue("@name", name);
            cmd.Parameters.AddWithValue("@tabs", JsonSerializer.Serialize(tabs));
            cmd.Parameters.AddWithValue("@state", windowState);
            cmd.Parameters.AddWithValue("@now", DateTime.UtcNow.ToString("o"));
            cmd.Parameters.AddWithValue("@auto", isAuto ? 1 : 0);
            await cmd.ExecuteNonQueryAsync();
            using var idCmd = _conn.CreateCommand();
            idCmd.CommandText = "SELECT last_insert_rowid();";
            return Convert.ToInt32(await idCmd.ExecuteScalarAsync());
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<List<SessionRecord>> GetSessionsAsync(int limit = 50)
    {
        await _lock.WaitAsync();
        try
        {
            var results = new List<SessionRecord>();
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = """
                SELECT id, name, tabs_json, window_state, created_at, is_auto FROM sessions
                ORDER BY created_at DESC LIMIT @limit;
                """;
            cmd.Parameters.AddWithValue("@limit", limit);
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                results.Add(new SessionRecord
                {
                    Id = reader.GetInt32(0),
                    Name = reader.GetString(1),
                    Tabs = JsonSerializer.Deserialize<List<SessionTab>>(reader.GetString(2)) ?? new(),
                    WindowState = reader.GetString(3),
                    CreatedAt = DateTime.Parse(reader.GetString(4)),
                    IsAuto = reader.GetInt32(5) == 1
                });
            }
            return results;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task DeleteSessionAsync(int id)
    {
        await _lock.WaitAsync();
        try
        {
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = "DELETE FROM sessions WHERE id = @id;";
            cmd.Parameters.AddWithValue("@id", id);
            await cmd.ExecuteNonQueryAsync();
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task PruneAutoSessionsAsync(int keepCount = 10)
    {
        await _lock.WaitAsync();
        try
        {
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = """
                DELETE FROM sessions WHERE is_auto = 1 AND id NOT IN (
                    SELECT id FROM sessions WHERE is_auto = 1 ORDER BY created_at DESC LIMIT @keep
                );
                """;
            cmd.Parameters.AddWithValue("@keep", keepCount);
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

public sealed class SessionTab
{
    public string Url { get; set; } = "";
    public string Title { get; set; } = "";
    public bool IsActive { get; set; }
    public int Index { get; set; }
}

public sealed class SessionRecord
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public List<SessionTab> Tabs { get; set; } = new();
    public string WindowState { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public bool IsAuto { get; set; }
}