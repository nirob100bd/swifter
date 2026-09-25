using System.Text.Json;

namespace Swifter.Core.TabEngine;

public sealed class SandboxManager
{
    private static SandboxManager? _instance;
    private readonly List<SandboxProfile> _profiles = new();
    private readonly string _profilesDir;

    public static SandboxManager Instance => _instance ??= new SandboxManager();

    public IReadOnlyList<SandboxProfile> Profiles => _profiles.AsReadOnly();

    private SandboxManager()
    {
        _profilesDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Swifter", "Profiles");
        Directory.CreateDirectory(_profilesDir);
        LoadProfiles();
        if (_profiles.Count == 0)
        {
            _profiles.Add(new SandboxProfile
            {
                Id = "default",
                Name = "Default",
                DataDirectory = Path.Combine(_profilesDir, "default"),
                IsDefault = true,
                CreatedAt = DateTime.UtcNow
            });
            Directory.CreateDirectory(_profiles[0].DataDirectory);
        }
    }

    private void LoadProfiles()
    {
        var configPath = Path.Combine(_profilesDir, "profiles.json");
        if (!File.Exists(configPath)) return;
        try
        {
            var json = File.ReadAllText(configPath);
            var profiles = JsonSerializer.Deserialize<List<SandboxProfile>>(json);
            if (profiles != null) _profiles.AddRange(profiles);
        }
        catch
        {
        }
    }

    public SandboxProfile CreateProfile(string name, string? color = null)
    {
        var id = Guid.NewGuid().ToString("N")[..8];
        var profile = new SandboxProfile
        {
            Id = id,
            Name = name,
            DataDirectory = Path.Combine(_profilesDir, id),
            Color = color ?? $"#{Random.Shared.Next(0x1000000):X6}",
            CreatedAt = DateTime.UtcNow
        };
        Directory.CreateDirectory(profile.DataDirectory);
        Directory.CreateDirectory(Path.Combine(profile.DataDirectory, "Cache"));
        Directory.CreateDirectory(Path.Combine(profile.DataDirectory, "Cookies"));
        Directory.CreateDirectory(Path.Combine(profile.DataDirectory, "Storage"));
        _profiles.Add(profile);
        SaveProfiles();
        return profile;
    }

    public void DeleteProfile(string profileId)
    {
        var profile = _profiles.FirstOrDefault(p => p.Id == profileId);
        if (profile == null || profile.IsDefault) return;
        _profiles.Remove(profile);
        try
        {
            if (Directory.Exists(profile.DataDirectory))
                Directory.Delete(profile.DataDirectory, true);
        }
        catch
        {
        }
        SaveProfiles();
    }

    public SandboxProfile? GetProfile(string profileId)
    {
        return _profiles.FirstOrDefault(p => p.Id == profileId);
    }

    public string GetProfileDataDir(string profileId)
    {
        var profile = _profiles.FirstOrDefault(p => p.Id == profileId);
        return profile?.DataDirectory ?? Path.Combine(_profilesDir, "default");
    }

    public void ClearProfileData(string profileId)
    {
        var profile = _profiles.FirstOrDefault(p => p.Id == profileId);
        if (profile == null) return;
        var cacheDir = Path.Combine(profile.DataDirectory, "Cache");
        if (Directory.Exists(cacheDir))
        {
            foreach (var file in Directory.GetFiles(cacheDir))
            {
                try { File.Delete(file); } catch { }
            }
        }
    }

    private void SaveProfiles()
    {
        var configPath = Path.Combine(_profilesDir, "profiles.json");
        var json = JsonSerializer.Serialize(_profiles, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(configPath, json);
    }

    public string GenerateProfilesPageHtml()
    {
        return $"""
            <html><head><style>
            body {{ font-family: 'Segoe UI', sans-serif; background: #1a1a2e; color: #e0e0e0; padding: 32px; }}
            .profile {{ background: #22223a; border-radius: 12px; padding: 20px; margin: 12px 0; display: flex; align-items: center; }}
            .avatar {{ width: 48px; height: 48px; border-radius: 50%; margin-right: 16px; display: flex; align-items: center; justify-content: center; font-size: 20px; font-weight: bold; color: #fff; }}
            .info {{ flex: 1; }}
            .name {{ font-size: 16px; font-weight: 600; color: #fff; }}
            .path {{ font-size: 11px; color: #666; margin-top: 2px; }}
            h1 {{ color: #0078d4; font-size: 24px; }}
            .badge {{ padding: 4px 12px; border-radius: 20px; font-size: 12px; }}
            .default {{ background: #1a3a1a; color: #3fb950; }}
            </style></head><body><h1>👤 Profiles</h1>
            {string.Join("", _profiles.Select(p => $"""
            <div class="profile">
                <div class="avatar" style="background:{p.Color}">{p.Name[0]}</div>
                <div class="info">
                    <div class="name">{p.Name}</div>
                    <div class="path">{p.DataDirectory}</div>
                </div>
                {(p.IsDefault ? """<span class="badge default">Default</span>""" : "")}
            </div>
            """))}
            </body></html>
            """;
    }
}

public sealed class SandboxProfile
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string DataDirectory { get; set; } = "";
    public string Color { get; set; } = "#0078D4";
    public bool IsDefault { get; set; }
    public DateTime CreatedAt { get; set; }
}