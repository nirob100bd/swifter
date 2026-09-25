using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using System.IO;

namespace Swifter.Core.Storage;

public sealed class VaultDatabase : IDisposable
{
    private readonly SqliteConnection _conn;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private static VaultDatabase? _instance;

    public static VaultDatabase Instance => _instance ??= new VaultDatabase();

    public VaultDatabase()
    {
        var dbPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Swifter", "vault.db");
        Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);
        _conn = new SqliteConnection($"Data Source={dbPath}");
        _conn.Open();
        Initialize();
    }

    private void Initialize()
    {
        using var cmd = _conn.CreateCommand();
        cmd.CommandText = """
            CREATE TABLE IF NOT EXISTS credentials (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                origin TEXT NOT NULL,
                username TEXT NOT NULL,
                password_encrypted BLOB NOT NULL,
                notes TEXT DEFAULT '',
                created_at TEXT NOT NULL,
                updated_at TEXT NOT NULL
            );
            CREATE INDEX IF NOT EXISTS idx_credentials_origin ON credentials(origin);
            CREATE TABLE IF NOT EXISTS credit_cards (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                cardholder TEXT NOT NULL,
                number_encrypted BLOB NOT NULL,
                expiry_month INTEGER NOT NULL,
                expiry_year INTEGER NOT NULL,
                cvv_encrypted BLOB NOT NULL,
                created_at TEXT NOT NULL
            );
            CREATE TABLE IF NOT EXISTS form_data (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                field_name TEXT NOT NULL,
                field_value_encrypted BLOB NOT NULL,
                origin TEXT NOT NULL,
                created_at TEXT NOT NULL
            );
            """;
        cmd.ExecuteNonQuery();
    }

    private byte[] Protect(byte[] data)
    {
        return ProtectedData.Protect(data, null, DataProtectionScope.CurrentUser);
    }

    private byte[] Unprotect(byte[] data)
    {
        return ProtectedData.Unprotect(data, null, DataProtectionScope.CurrentUser);
    }

    public async Task SaveCredentialAsync(string origin, string username, string password, string notes = "")
    {
        await _lock.WaitAsync();
        try
        {
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = """
                INSERT INTO credentials (origin, username, password_encrypted, notes, created_at, updated_at)
                VALUES (@origin, @user, @pass, @notes, @now, @now)
                ON CONFLICT(origin, username) DO UPDATE SET
                    password_encrypted = @pass, notes = @notes, updated_at = @now;
                """;
            cmd.Parameters.AddWithValue("@origin", origin);
            cmd.Parameters.AddWithValue("@user", username);
            cmd.Parameters.AddWithValue("@pass", Protect(Encoding.UTF8.GetBytes(password)));
            cmd.Parameters.AddWithValue("@notes", notes);
            cmd.Parameters.AddWithValue("@now", DateTime.UtcNow.ToString("o"));
            await cmd.ExecuteNonQueryAsync();
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<List<CredentialEntry>> GetCredentialsAsync(string origin)
    {
        await _lock.WaitAsync();
        try
        {
            var results = new List<CredentialEntry>();
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = """
                SELECT id, origin, username, password_encrypted, notes, created_at FROM credentials
                WHERE origin = @origin ORDER BY updated_at DESC;
                """;
            cmd.Parameters.AddWithValue("@origin", origin);
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var encryptedBytes = (byte[])reader.GetValue(3);
                results.Add(new CredentialEntry
                {
                    Id = reader.GetInt32(0),
                    Origin = reader.GetString(1),
                    Username = reader.GetString(2),
                    Password = Encoding.UTF8.GetString(Unprotect(encryptedBytes)),
                    Notes = reader.IsDBNull(4) ? "" : reader.GetString(4),
                    CreatedAt = DateTime.Parse(reader.GetString(5))
                });
            }
            return results;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task SaveCreditCardAsync(string cardholder, string number, int month, int year, string cvv)
    {
        await _lock.WaitAsync();
        try
        {
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = """
                INSERT INTO credit_cards (cardholder, number_encrypted, expiry_month, expiry_year, cvv_encrypted, created_at)
                VALUES (@ch, @num, @month, @year, @cvv, @now);
                """;
            cmd.Parameters.AddWithValue("@ch", cardholder);
            cmd.Parameters.AddWithValue("@num", Protect(Encoding.UTF8.GetBytes(number)));
            cmd.Parameters.AddWithValue("@month", month);
            cmd.Parameters.AddWithValue("@year", year);
            cmd.Parameters.AddWithValue("@cvv", Protect(Encoding.UTF8.GetBytes(cvv)));
            cmd.Parameters.AddWithValue("@now", DateTime.UtcNow.ToString("o"));
            await cmd.ExecuteNonQueryAsync();
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task SaveFormDataAsync(string fieldName, string fieldValue, string origin)
    {
        await _lock.WaitAsync();
        try
        {
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = """
                INSERT INTO form_data (field_name, field_value_encrypted, origin, created_at)
                VALUES (@name, @value, @origin, @now);
                """;
            cmd.Parameters.AddWithValue("@name", fieldName);
            cmd.Parameters.AddWithValue("@value", Protect(Encoding.UTF8.GetBytes(fieldValue)));
            cmd.Parameters.AddWithValue("@origin", origin);
            cmd.Parameters.AddWithValue("@now", DateTime.UtcNow.ToString("o"));
            await cmd.ExecuteNonQueryAsync();
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task DeleteCredentialAsync(int id)
    {
        await _lock.WaitAsync();
        try
        {
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = "DELETE FROM credentials WHERE id = @id;";
            cmd.Parameters.AddWithValue("@id", id);
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
            cmd.CommandText = "DELETE FROM credentials; DELETE FROM credit_cards; DELETE FROM form_data;";
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

public sealed class CredentialEntry
{
    public int Id { get; set; }
    public string Origin { get; set; } = "";
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
    public string Notes { get; set; } = "";
    public DateTime CreatedAt { get; set; }
}