using Microsoft.Data.Sqlite;

namespace Swifter.Core.Storage;

public sealed class BookmarksDatabase : IDisposable
{
    private readonly SqliteConnection _conn;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public BookmarksDatabase()
    {
        var dbPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Swifter", "bookmarks.db");
        Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);
        _conn = new SqliteConnection($"Data Source={dbPath}");
        _conn.Open();
        Initialize();
    }

    private void Initialize()
    {
        using var cmd = _conn.CreateCommand();
        cmd.CommandText = """
            CREATE TABLE IF NOT EXISTS bookmarks (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                url TEXT NOT NULL,
                title TEXT NOT NULL DEFAULT '',
                folder TEXT NOT NULL DEFAULT 'Bookmarks Bar',
                favicon_url TEXT DEFAULT NULL,
                position INTEGER NOT NULL DEFAULT 0,
                created_at TEXT NOT NULL
            );
            CREATE INDEX IF NOT EXISTS idx_bookmarks_folder ON bookmarks(folder);
            CREATE INDEX IF NOT EXISTS idx_bookmarks_position ON bookmarks(position);
            """;
        cmd.ExecuteNonQuery();
    }

    public async Task<int> AddAsync(string url, string title, string folder = "Bookmarks Bar")
    {
        await _lock.WaitAsync();
        try
        {
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = """
                SELECT COALESCE(MAX(position), 0) + 1 FROM bookmarks WHERE folder = @folder;
                """;
            cmd.Parameters.AddWithValue("@folder", folder);
            var pos = Convert.ToInt32(await cmd.ExecuteScalarAsync());
            using var insertCmd = _conn.CreateCommand();
            insertCmd.CommandText = """
                INSERT INTO bookmarks (url, title, folder, position, created_at)
                VALUES (@url, @title, @folder, @pos, @now);
                """;
            insertCmd.Parameters.AddWithValue("@url", url);
            insertCmd.Parameters.AddWithValue("@title", title);
            insertCmd.Parameters.AddWithValue("@folder", folder);
            insertCmd.Parameters.AddWithValue("@pos", pos);
            insertCmd.Parameters.AddWithValue("@now", DateTime.UtcNow.ToString("o"));
            await insertCmd.ExecuteNonQueryAsync();
            using var idCmd = _conn.CreateCommand();
            idCmd.CommandText = "SELECT last_insert_rowid();";
            return Convert.ToInt32(await idCmd.ExecuteScalarAsync());
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<List<BookmarkEntry>> GetAllAsync()
    {
        await _lock.WaitAsync();
        try
        {
            var results = new List<BookmarkEntry>();
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = """
                SELECT id, url, title, folder, favicon_url, position, created_at FROM bookmarks
                ORDER BY folder, position;
                """;
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                results.Add(new BookmarkEntry
                {
                    Id = reader.GetInt32(0),
                    Url = reader.GetString(1),
                    Title = reader.GetString(2),
                    Folder = reader.GetString(3),
                    FaviconUrl = reader.IsDBNull(4) ? null : reader.GetString(4),
                    Position = reader.GetInt32(5),
                    CreatedAt = DateTime.Parse(reader.GetString(6))
                });
            }
            return results;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<List<BookmarkEntry>> GetByFolderAsync(string folder)
    {
        await _lock.WaitAsync();
        try
        {
            var results = new List<BookmarkEntry>();
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = """
                SELECT id, url, title, folder, favicon_url, position, created_at FROM bookmarks
                WHERE folder = @folder ORDER BY position;
                """;
            cmd.Parameters.AddWithValue("@folder", folder);
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                results.Add(new BookmarkEntry
                {
                    Id = reader.GetInt32(0),
                    Url = reader.GetString(1),
                    Title = reader.GetString(2),
                    Folder = reader.GetString(3),
                    FaviconUrl = reader.IsDBNull(4) ? null : reader.GetString(4),
                    Position = reader.GetInt32(5),
                    CreatedAt = DateTime.Parse(reader.GetString(6))
                });
            }
            return results;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task UpdateAsync(int id, string url, string title, string folder)
    {
        await _lock.WaitAsync();
        try
        {
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = """
                UPDATE bookmarks SET url = @url, title = @title, folder = @folder WHERE id = @id;
                """;
            cmd.Parameters.AddWithValue("@id", id);
            cmd.Parameters.AddWithValue("@url", url);
            cmd.Parameters.AddWithValue("@title", title);
            cmd.Parameters.AddWithValue("@folder", folder);
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
            cmd.CommandText = "DELETE FROM bookmarks WHERE id = @id;";
            cmd.Parameters.AddWithValue("@id", id);
            await cmd.ExecuteNonQueryAsync();
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<List<string>> GetFoldersAsync()
    {
        await _lock.WaitAsync();
        try
        {
            var results = new List<string>();
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = "SELECT DISTINCT folder FROM bookmarks ORDER BY folder;";
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                results.Add(reader.GetString(0));
            }
            return results;
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

public sealed class BookmarkEntry
{
    public int Id { get; set; }
    public string Url { get; set; } = "";
    public string Title { get; set; } = "";
    public string Folder { get; set; } = "Bookmarks Bar";
    public string? FaviconUrl { get; set; }
    public int Position { get; set; }
    public DateTime CreatedAt { get; set; }
}