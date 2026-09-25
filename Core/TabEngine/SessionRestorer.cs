using System.Text.Json;
using Swifter.Core.Storage;

namespace Swifter.Core.TabEngine;

public sealed class SessionRestorer : IDisposable
{
    private static SessionRestorer? _instance;
    private Timer? _snapshotTimer;
    private readonly string _sessionDir;

    public static SessionRestorer Instance => _instance ??= new SessionRestorer();

    private SessionRestorer()
    {
        _sessionDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Swifter", "Sessions");
        Directory.CreateDirectory(_sessionDir);
    }

    public void StartAutoSave(int intervalSeconds = 30)
    {
        _snapshotTimer?.Dispose();
        _snapshotTimer = new Timer(_ => SaveSnapshot(), null, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(intervalSeconds));
    }

    public void StopAutoSave()
    {
        _snapshotTimer?.Dispose();
        _snapshotTimer = null;
    }

    public void SaveSnapshot()
    {
        try
        {
            var tabs = TabManager.Instance.GetTabSnapshot();
            var snapshot = new SessionSnapshot
            {
                Tabs = tabs.Select(t => new SessionTab
                {
                    Url = t.Url,
                    Title = t.Title,
                    IsActive = t.IsActive,
                    Index = tabs.IndexOf(t)
                }).ToList(),
                SavedAt = DateTime.UtcNow
            };
            var json = JsonSerializer.Serialize(snapshot, new JsonSerializerOptions { WriteIndented = true });
            var tempPath = Path.Combine(_sessionDir, "session_current.json.tmp");
            var finalPath = Path.Combine(_sessionDir, "session_current.json");
            File.WriteAllText(tempPath, json);
            if (File.Exists(finalPath)) File.Delete(finalPath);
            File.Move(tempPath, finalPath);
        }
        catch
        {
        }
    }

    public List<SessionTab>? RestoreLastSession()
    {
        try
        {
            var path = Path.Combine(_sessionDir, "session_current.json");
            if (!File.Exists(path)) return null;
            var json = File.ReadAllText(path);
            var snapshot = JsonSerializer.Deserialize<SessionSnapshot>(json);
            return snapshot?.Tabs;
        }
        catch
        {
            return null;
        }
    }

    public void ClearSession()
    {
        try
        {
            var path = Path.Combine(_sessionDir, "session_current.json");
            if (File.Exists(path)) File.Delete(path);
        }
        catch
        {
        }
    }

    public List<SessionFileInfo> GetSessionFiles()
    {
        return Directory.GetFiles(_sessionDir, "*.json")
            .Select(f => new FileInfo(f))
            .Select(fi => new SessionFileInfo
            {
                FileName = fi.Name,
                SizeBytes = fi.Length,
                ModifiedAt = fi.LastWriteTimeUtc
            })
            .OrderByDescending(f => f.ModifiedAt)
            .ToList();
    }

    public void Dispose()
    {
        _snapshotTimer?.Dispose();
    }
}

public sealed class SessionSnapshot
{
    public List<SessionTab> Tabs { get; set; } = new();
    public DateTime SavedAt { get; set; }
}

public sealed class SessionFileInfo
{
    public string FileName { get; set; } = "";
    public long SizeBytes { get; set; }
    public DateTime ModifiedAt { get; set; }
}