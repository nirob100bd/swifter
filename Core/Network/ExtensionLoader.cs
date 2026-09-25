namespace Swifter.Core.Network;

public sealed class ExtensionLoader
{
    private static ExtensionLoader? _instance;
    private readonly List<Extension> _extensions = new();
    private readonly string _extensionsDir;

    public static ExtensionLoader Instance => _instance ??= new ExtensionLoader();

    public IReadOnlyList<Extension> Extensions => _extensions.AsReadOnly();

    public event EventHandler<Extension>? ExtensionLoaded;
    public event EventHandler<Extension>? ExtensionUnloaded;

    private ExtensionLoader()
    {
        _extensionsDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Swifter", "Extensions");
        Directory.CreateDirectory(_extensionsDir);
        LoadExtensions();
    }

    private void LoadExtensions()
    {
        foreach (var dir in Directory.GetDirectories(_extensionsDir))
        {
            var manifestPath = Path.Combine(dir, "manifest.json");
            if (!File.Exists(manifestPath)) continue;
            try
            {
                var json = File.ReadAllText(manifestPath);
                var manifest = System.Text.Json.JsonSerializer.Deserialize<ExtensionManifest>(json);
                if (manifest == null) continue;
                var ext = new Extension
                {
                    Id = Path.GetFileName(dir),
                    Name = manifest.Name,
                    Version = manifest.Version,
                    Description = manifest.Description,
                    Author = manifest.Author,
                    Directory = dir,
                    Manifest = manifest,
                    IsEnabled = true
                };
                _extensions.Add(ext);
                ExtensionLoaded?.Invoke(this, ext);
            }
            catch
            {
            }
        }
    }

    public Extension? LoadExtension(string extensionDir)
    {
        var manifestPath = Path.Combine(extensionDir, "manifest.json");
        if (!File.Exists(manifestPath)) return null;
        try
        {
            var json = File.ReadAllText(manifestPath);
            var manifest = System.Text.Json.JsonSerializer.Deserialize<ExtensionManifest>(json);
            if (manifest == null) return null;
            var ext = new Extension
            {
                Id = Path.GetFileName(extensionDir),
                Name = manifest.Name,
                Version = manifest.Version,
                Description = manifest.Description,
                Author = manifest.Author,
                Directory = extensionDir,
                Manifest = manifest,
                IsEnabled = true
            };
            _extensions.Add(ext);
            ExtensionLoaded?.Invoke(this, ext);
            return ext;
        }
        catch
        {
            return null;
        }
    }

    public void UnloadExtension(string extensionId)
    {
        var ext = _extensions.FirstOrDefault(e => e.Id == extensionId);
        if (ext == null) return;
        ext.IsEnabled = false;
        ExtensionUnloaded?.Invoke(this, ext);
    }

    public string? GetContentScripts(string extensionId)
    {
        var ext = _extensions.FirstOrDefault(e => e.Id == extensionId && e.IsEnabled);
        if (ext?.Manifest?.ContentScripts == null) return null;
        var scripts = new List<string>();
        foreach (var script in ext.Manifest.ContentScripts)
        {
            var path = Path.Combine(ext.Directory, script);
            if (File.Exists(path)) scripts.Add(File.ReadAllText(path));
        }
        return scripts.Count > 0 ? string.Join("\n", scripts) : null;
    }

    public string? GetBackgroundScript(string extensionId)
    {
        var ext = _extensions.FirstOrDefault(e => e.Id == extensionId && e.IsEnabled);
        if (ext?.Manifest?.BackgroundScript == null) return null;
        var path = Path.Combine(ext.Directory, ext.Manifest.BackgroundScript);
        return File.Exists(path) ? File.ReadAllText(path) : null;
    }

    public string GenerateExtensionsPageHtml()
    {
        var cards = string.Join("", _extensions.Select(e =>
        {
            var badgeClass = e.IsEnabled ? "badge-on" : "badge-off";
            var badgeText = e.IsEnabled ? "Enabled" : "Disabled";
            return $"""
                <div class="ext">
                    <div class="ext-info">
                        <div class="ext-name">{e.Name}</div>
                        <div class="ext-desc">{e.Description}</div>
                        <div class="ext-ver">v{e.Version} by {e.Author}</div>
                    </div>
                    <span class="badge {badgeClass}">{badgeText}</span>
                </div>
                """;
        }));
        return $$"""
            <html><head><style>
            body { font-family: 'Segoe UI', sans-serif; background: #1a1a2e; color: #e0e0e0; padding: 32px; }
            .ext { background: #22223a; border-radius: 12px; padding: 20px; margin: 12px 0; display: flex; align-items: center; }
            .ext-info { flex: 1; }
            .ext-name { font-size: 16px; font-weight: 600; color: #fff; }
            .ext-desc { font-size: 13px; color: #888; margin-top: 4px; }
            .ext-ver { font-size: 11px; color: #0078d4; margin-top: 2px; }
            h1 { color: #0078d4; font-size: 24px; }
            .badge { padding: 4px 12px; border-radius: 20px; font-size: 12px; font-weight: 600; }
            .badge-on { background: #1a3a1a; color: #3fb950; }
            .badge-off { background: #3a1a1a; color: #f85149; }
            </style></head><body><h1>🧩 Extensions</h1>
            {{cards}}
            </body></html>
            """;
    }
}

public sealed class Extension
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Version { get; set; } = "";
    public string Description { get; set; } = "";
    public string Author { get; set; } = "";
    public string Directory { get; set; } = "";
    public ExtensionManifest? Manifest { get; set; }
    public bool IsEnabled { get; set; }
}

public sealed class ExtensionManifest
{
    [System.Text.Json.Serialization.JsonPropertyName("name")]
    public string Name { get; set; } = "";
    [System.Text.Json.Serialization.JsonPropertyName("version")]
    public string Version { get; set; } = "";
    [System.Text.Json.Serialization.JsonPropertyName("description")]
    public string Description { get; set; } = "";
    [System.Text.Json.Serialization.JsonPropertyName("author")]
    public string Author { get; set; } = "";
    [System.Text.Json.Serialization.JsonPropertyName("content_scripts")]
    public List<string>? ContentScripts { get; set; }
    [System.Text.Json.Serialization.JsonPropertyName("background_script")]
    public string? BackgroundScript { get; set; }
    [System.Text.Json.Serialization.JsonPropertyName("permissions")]
    public List<string>? Permissions { get; set; }
}