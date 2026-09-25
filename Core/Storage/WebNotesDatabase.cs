using Microsoft.Data.Sqlite;
using System.IO;

namespace Swifter.Core.Storage;

public sealed class WebNotesDatabase : IDisposable
{
    private readonly SqliteConnection _conn;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public WebNotesDatabase()
    {
        var dbPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Swifter", "webnotes.db");
        Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);
        _conn = new SqliteConnection($"Data Source={dbPath}");
        _conn.Open();
        Initialize();
    }

    private void Initialize()
    {
        using var cmd = _conn.CreateCommand();
        cmd.CommandText = """
            CREATE TABLE IF NOT EXISTS notes (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                title TEXT NOT NULL DEFAULT '',
                content TEXT NOT NULL DEFAULT '',
                source_url TEXT DEFAULT NULL,
                tags TEXT DEFAULT '',
                color TEXT DEFAULT '#FFD700',
                is_pinned INTEGER NOT NULL DEFAULT 0,
                created_at TEXT NOT NULL,
                updated_at TEXT NOT NULL
            );
            CREATE INDEX IF NOT EXISTS idx_notes_created ON notes(created_at DESC);
            CREATE INDEX IF NOT EXISTS idx_notes_pinned ON notes(is_pinned DESC);
            """;
        cmd.ExecuteNonQuery();
    }

    public async Task<int> CreateAsync(string title, string content, string? sourceUrl = null, string tags = "", string color = "#FFD700")
    {
        await _lock.WaitAsync();
        try
        {
            using var cmd = _conn.CreateCommand();
            var now = DateTime.UtcNow.ToString("o");
            cmd.CommandText = """
                INSERT INTO notes (title, content, source_url, tags, color, created_at, updated_at)
                VALUES (@title, @content, @url, @tags, @color, @now, @now);
                """;
            cmd.Parameters.AddWithValue("@title", title);
            cmd.Parameters.AddWithValue("@content", content);
            cmd.Parameters.AddWithValue("@url", (object?)sourceUrl ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@tags", tags);
            cmd.Parameters.AddWithValue("@color", color);
            cmd.Parameters.AddWithValue("@now", now);
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

    public async Task UpdateAsync(int id, string title, string content, string tags = "", string color = "#FFD700")
    {
        await _lock.WaitAsync();
        try
        {
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = """
                UPDATE notes SET title = @title, content = @content, tags = @tags, color = @color, updated_at = @now WHERE id = @id;
                """;
            cmd.Parameters.AddWithValue("@id", id);
            cmd.Parameters.AddWithValue("@title", title);
            cmd.Parameters.AddWithValue("@content", content);
            cmd.Parameters.AddWithValue("@tags", tags);
            cmd.Parameters.AddWithValue("@color", color);
            cmd.Parameters.AddWithValue("@now", DateTime.UtcNow.ToString("o"));
            await cmd.ExecuteNonQueryAsync();
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<List<NoteEntry>> GetAllAsync()
    {
        await _lock.WaitAsync();
        try
        {
            var results = new List<NoteEntry>();
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = """
                SELECT id, title, content, source_url, tags, color, is_pinned, created_at, updated_at FROM notes
                ORDER BY is_pinned DESC, updated_at DESC;
                """;
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                results.Add(new NoteEntry
                {
                    Id = reader.GetInt32(0),
                    Title = reader.GetString(1),
                    Content = reader.GetString(2),
                    SourceUrl = reader.IsDBNull(3) ? null : reader.GetString(3),
                    Tags = reader.GetString(4),
                    Color = reader.GetString(5),
                    IsPinned = reader.GetInt32(6) == 1,
                    CreatedAt = DateTime.Parse(reader.GetString(7)),
                    UpdatedAt = DateTime.Parse(reader.GetString(8))
                });
            }
            return results;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task TogglePinAsync(int id)
    {
        await _lock.WaitAsync();
        try
        {
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = "UPDATE notes SET is_pinned = 1 - is_pinned WHERE id = @id;";
            cmd.Parameters.AddWithValue("@id", id);
            await cmd.ExecuteNonQueryAsync();
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task DeleteAsync(int id)
    {
        await _lock.WaitAsync();
        try
        {
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = "DELETE FROM notes WHERE id = @id;";
            cmd.Parameters.AddWithValue("@id", id);
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

public sealed class NoteEntry
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public string Content { get; set; } = "";
    public string? SourceUrl { get; set; }
    public string Tags { get; set; } = "";
    public string Color { get; set; } = "#FFD700";
    public bool IsPinned { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}