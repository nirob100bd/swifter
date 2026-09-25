using System.IO;
using System.Text.Json;

namespace Swifter.Core.Config;

public sealed class SettingsManager
{
    private static SettingsManager? _instance;
    private SettingsModel _settings;
    private readonly string _settingsPath;
    private readonly FileSystemWatcher _watcher;
    private readonly SemaphoreSlim _saveLock = new(1, 1);
    private DateTime _lastWrite = DateTime.MinValue;

    public static SettingsManager Instance => _instance ??= new SettingsManager();

    public SettingsModel Settings => _settings;

    public event EventHandler<SettingsModel>? SettingsChanged;

    private SettingsManager()
    {
        var appData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Swifter");
        Directory.CreateDirectory(appData);
        _settingsPath = Path.Combine(appData, "settings.json");
        _settings = LoadSettings();
        _watcher = new FileSystemWatcher(appData, "settings.json")
        {
            NotifyFilter = NotifyFilters.LastWrite,
            EnableRaisingEvents = true
        };
        _watcher.Changed += OnSettingsFileChanged;
    }

    private SettingsModel LoadSettings()
    {
        try
        {
            if (File.Exists(_settingsPath))
            {
                var json = File.ReadAllText(_settingsPath);
                return JsonSerializer.Deserialize<SettingsModel>(json) ?? new SettingsModel();
            }
        }
        catch
        {
        }
        return new SettingsModel();
    }

    public async Task SaveAsync()
    {
        await _saveLock.WaitAsync();
        try
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(_settings, options);
            await File.WriteAllTextAsync(_settingsPath, json);
            _lastWrite = File.GetLastWriteTimeUtc(_settingsPath);
        }
        finally
        {
            _saveLock.Release();
        }
    }

    public void Save()
    {
        SaveAsync().GetAwaiter().GetResult();
    }

    private void OnSettingsFileChanged(object sender, FileSystemEventArgs e)
    {
        var writeTime = File.GetLastWriteTimeUtc(e.FullPath);
        if (writeTime <= _lastWrite) return;
        _lastWrite = writeTime;
        Task.Delay(100).ContinueWith(_ =>
        {
            _settings = LoadSettings();
            SettingsChanged?.Invoke(this, _settings);
        });
    }

    public T GetValue<T>(string key, T defaultValue)
    {
        try
        {
            var props = typeof(SettingsModel).GetProperties();
            var prop = props.FirstOrDefault(p => p.Name.Equals(key, StringComparison.OrdinalIgnoreCase));
            if (prop?.GetValue(_settings) is T val) return val;
        }
        catch
        {
        }
        return defaultValue;
    }

    public void SetValue<T>(string key, T value)
    {
        try
        {
            var props = typeof(SettingsModel).GetProperties();
            var prop = props.FirstOrDefault(p => p.Name.Equals(key, StringComparison.OrdinalIgnoreCase));
            prop?.SetValue(_settings, value);
            Save();
        }
        catch
        {
        }
    }

    public void Reset()
    {
        _settings = new SettingsModel();
        Save();
    }

    public void Dispose()
    {
        _watcher.Dispose();
        _saveLock.Dispose();
    }
}