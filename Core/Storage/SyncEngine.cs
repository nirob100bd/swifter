using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Swifter.Core.Storage;

public sealed class SyncEngine
{
    private static SyncEngine? _instance;
    private readonly string _syncDir;
    private readonly byte[] _encryptionKey;
    private Timer? _syncTimer;

    public static SyncEngine Instance => _instance ??= new SyncEngine();

    public event EventHandler<SyncStatus>? SyncCompleted;

    private SyncEngine()
    {
        _syncDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Swifter", "Sync");
        Directory.CreateDirectory(_syncDir);
        _encryptionKey = DeriveKey();
    }

    private byte[] DeriveKey()
    {
        var machineId = Environment.MachineName + Environment.UserName;
        using var deriveBytes = new Rfc2898DeriveBytes(machineId, Encoding.UTF8.GetBytes("SwifterSyncSalt2026"), 100000, HashAlgorithmName.SHA256);
        return deriveBytes.GetBytes(32);
    }

    public void StartAutoSync(int intervalMinutes = 15)
    {
        _syncTimer?.Dispose();
        _syncTimer = new Timer(async _ => await SyncAsync(), null, TimeSpan.Zero, TimeSpan.FromMinutes(intervalMinutes));
    }

    public void StopAutoSync()
    {
        _syncTimer?.Dispose();
        _syncTimer = null;
    }

    public async Task<SyncStatus> SyncAsync()
    {
        var status = new SyncStatus();
        try
        {
            await ExportDataAsync("bookmarks", await GetBookmarksDataAsync());
            await ExportDataAsync("history", await GetHistoryDataAsync());
            await ExportDataAsync("settings", await GetSettingsDataAsync());
            status.Success = true;
            status.Timestamp = DateTime.UtcNow;
        }
        catch (Exception ex)
        {
            status.Success = false;
            status.Error = ex.Message;
        }
        SyncCompleted?.Invoke(this, status);
        return status;
    }

    private async Task<string> GetBookmarksDataAsync()
    {
        var bookmarks = await BookmarksDatabase.Instance.GetAllAsync();
        return JsonSerializer.Serialize(bookmarks, new JsonSerializerOptions { WriteIndented = true });
    }

    private async Task<string> GetHistoryDataAsync()
    {
        var history = await HistoryDatabase.Instance.SearchAsync("", 1000);
        return JsonSerializer.Serialize(history, new JsonSerializerOptions { WriteIndented = true });
    }

    private async Task<string> GetSettingsDataAsync()
    {
        var settingsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Swifter", "settings.json");
        return File.Exists(settingsPath) ? await File.ReadAllTextAsync(settingsPath) : "{}";
    }

    private async Task ExportDataAsync(string name, string jsonData)
    {
        var data = Encoding.UTF8.GetBytes(jsonData);
        using var aes = Aes.Create();
        aes.Key = _encryptionKey;
        aes.GenerateIV();
        using var encryptor = aes.CreateEncryptor();
        var encrypted = encryptor.TransformFinalBlock(data, 0, data.Length);
        var output = new byte[aes.IV.Length + encrypted.Length];
        Buffer.BlockCopy(aes.IV, 0, output, 0, aes.IV.Length);
        Buffer.BlockCopy(encrypted, 0, output, aes.IV.Length, encrypted.Length);
        var filePath = Path.Combine(_syncDir, $"{name}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.enc");
        await File.WriteAllBytesAsync(filePath, output);
    }

    public async Task<string> ImportDataAsync(string filePath)
    {
        var encrypted = await File.ReadAllBytesAsync(filePath);
        using var aes = Aes.Create();
        aes.Key = _encryptionKey;
        var iv = new byte[16];
        Buffer.BlockCopy(encrypted, 0, iv, 0, 16);
        aes.IV = iv;
        using var decryptor = aes.CreateDecryptor();
        var decrypted = decryptor.TransformFinalBlock(encrypted, 16, encrypted.Length - 16);
        return Encoding.UTF8.GetString(decrypted);
    }

    public List<SyncFileInfo> GetSyncFiles()
    {
        return Directory.GetFiles(_syncDir, "*.enc")
            .Select(f => new FileInfo(f))
            .Select(fi => new SyncFileInfo
            {
                FileName = fi.Name,
                FilePath = fi.FullName,
                Size = fi.Length,
                CreatedAt = fi.CreationTimeUtc
            })
            .OrderByDescending(f => f.CreatedAt)
            .ToList();
    }

    public void Dispose()
    {
        _syncTimer?.Dispose();
    }
}

public sealed class SyncStatus
{
    public bool Success { get; set; }
    public DateTime? Timestamp { get; set; }
    public string? Error { get; set; }
}

public sealed class SyncFileInfo
{
    public string FileName { get; set; } = "";
    public string FilePath { get; set; } = "";
    public long Size { get; set; }
    public DateTime CreatedAt { get; set; }
}