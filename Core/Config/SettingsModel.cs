using System.Text.Json.Serialization;

namespace Swifter.Core.Config;

public sealed class SettingsModel
{
    [JsonPropertyName("general")]
    public GeneralSettings General { get; set; } = new();

    [JsonPropertyName("appearance")]
    public AppearanceSettings Appearance { get; set; } = new();

    [JsonPropertyName("privacy")]
    public PrivacySettings Privacy { get; set; } = new();

    [JsonPropertyName("network")]
    public NetworkSettings Network { get; set; } = new();

    [JsonPropertyName("performance")]
    public PerformanceSettings Performance { get; set; } = new();

    [JsonPropertyName("shortcuts")]
    public Dictionary<string, string> Shortcuts { get; set; } = new();

    [JsonPropertyName("search_engines")]
    public List<SearchEngineModel> SearchEngines { get; set; } = new();

    [JsonPropertyName("startup_pages")]
    public List<string> StartupPages { get; set; } = new();

    [JsonPropertyName("download_path")]
    public string DownloadPath { get; set; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");

    [JsonPropertyName("proxy")]
    public ProxySettings Proxy { get; set; } = new();

    [JsonPropertyName("dns")]
    public DnsSettings Dns { get; set; } = new();
}

public sealed class GeneralSettings
{
    [JsonPropertyName("homepage")]
    public string Homepage { get; set; } = "swifter://newtab";

    [JsonPropertyName("restore_tabs_on_start")]
    public bool RestoreTabsOnStart { get; set; } = true;

    [JsonPropertyName("default_search_engine")]
    public string DefaultSearchEngine { get; set; } = "https://www.google.com/search?q=";

    [JsonPropertyName("show_bookmarks_bar")]
    public bool ShowBookmarksBar { get; set; } = true;

    [JsonPropertyName("show_status_bar")]
    public bool ShowStatusBar { get; set; } = true;

    [JsonPropertyName("warn_on_close_multiple")]
    public bool WarnOnCloseMultiple { get; set; } = true;

    [JsonPropertyName("language")]
    public string Language { get; set; } = "en-US";

    [JsonPropertyName("spell_check")]
    public bool SpellCheck { get; set; } = true;
}

public sealed class AppearanceSettings
{
    [JsonPropertyName("theme")]
    public string Theme { get; set; } = "Dark";

    [JsonPropertyName("accent_color")]
    public string AccentColor { get; set; } = "#0078D4";

    [JsonPropertyName("glass_opacity")]
    public double GlassOpacity { get; set; } = 0.85;

    [JsonPropertyName("backdrop_type")]
    public string BackdropType { get; set; } = "Mica";

    [JsonPropertyName("tab_position")]
    public string TabPosition { get; set; } = "Top";

    [JsonPropertyName("font_family")]
    public string FontFamily { get; set; } = "Segoe UI Variable";

    [JsonPropertyName("font_size")]
    public int FontSize { get; set; } = 14;

    [JsonPropertyName("custom_css")]
    public string CustomCss { get; set; } = "";

    [JsonPropertyName("compact_mode")]
    public bool CompactMode { get; set; } = false;

    [JsonPropertyName("show_tab_thumbnails")]
    public bool ShowTabThumbnails { get; set; } = true;

    [JsonPropertyName("toolbar_layout")]
    public List<string> ToolbarLayout { get; set; } = new()
    {
        "Back", "Forward", "Refresh", "Home", "AddressBar", "Downloads",
        "Bookmarks", "History", "Settings", "NewTab"
    };
}

public sealed class PrivacySettings
{
    [JsonPropertyName("block_trackers")]
    public bool BlockTrackers { get; set; } = true;

    [JsonPropertyName("block_ads")]
    public bool BlockAds { get; set; } = true;

    [JsonPropertyName("block_popups")]
    public bool BlockPopups { get; set; } = true;

    [JsonPropertyName("do_not_track")]
    public bool DoNotTrack { get; set; } = true;

    [JsonPropertyName("clear_on_exit")]
    public bool ClearOnExit { get; set; } = false;

    [JsonPropertyName("cookie_policy")]
    public string CookiePolicy { get; set; } = "BlockThirdParty";

    [JsonPropertyName("fingerprint_protection")]
    public bool FingerprintProtection { get; set; } = true;

    [JsonPropertyName("https_only")]
    public bool HttpsOnly { get; set; } = true;

    [JsonPropertyName("webrtc_leak_protection")]
    public bool WebRtcLeakProtection { get; set; } = true;
}

public sealed class NetworkSettings
{
    [JsonPropertyName("max_concurrent_downloads")]
    public int MaxConcurrentDownloads { get; set; } = 5;

    [JsonPropertyName("download_threads")]
    public int DownloadThreads { get; set; } = 32;

    [JsonPropertyName("buffer_size_kb")]
    public int BufferSizeKB { get; set; } = 8192;

    [JsonPropertyName("connection_timeout_ms")]
    public int ConnectionTimeoutMs { get; set; } = 30000;

    [JsonPropertyName("preconnect")]
    public bool Preconnect { get; set; } = true;

    [JsonPropertyName("prefetch")]
    public bool Prefetch { get; set; } = true;

    [JsonPropertyName("turbo_mode")]
    public bool TurboMode { get; set; } = false;
}

public sealed class PerformanceSettings
{
    [JsonPropertyName("hardware_acceleration")]
    public bool HardwareAcceleration { get; set; } = true;

    [JsonPropertyName("memory_limit_mb")]
    public int MemoryLimitMB { get; set; } = 4096;

    [JsonPropertyName("tab_discard_timeout_min")]
    public int TabDiscardTimeoutMin { get; set; } = 30;

    [JsonPropertyName("max_background_tabs")]
    public int MaxBackgroundTabs { get; set; } = 50;

    [JsonPropertyName("process_count")]
    public int ProcessCount { get; set; } = 4;

    [JsonPropertyName("enable_tab_hibernation")]
    public bool EnableTabHibernation { get; set; } = true;
}

public sealed class ProxySettings
{
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = false;

    [JsonPropertyName("type")]
    public string Type { get; set; } = "SOCKS5";

    [JsonPropertyName("host")]
    public string Host { get; set; } = "";

    [JsonPropertyName("port")]
    public int Port { get; set; } = 1080;

    [JsonPropertyName("username")]
    public string Username { get; set; } = "";

    [JsonPropertyName("password")]
    public string Password { get; set; } = "";

    [JsonPropertyName("bypass_list")]
    public List<string> BypassList { get; set; } = new();
}

public sealed class DnsSettings
{
    [JsonPropertyName("over_https")]
    public bool OverHttps { get; set; } = false;

    [JsonPropertyName("provider")]
    public string Provider { get; set; } = "https://cloudflare-dns.com/dns-query";

    [JsonPropertyName("fallback")]
    public string Fallback { get; set; } = "https://dns.google/dns-query";
}

public sealed class SearchEngineModel
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("url")]
    public string Url { get; set; } = "";

    [JsonPropertyName("shortcut")]
    public string Shortcut { get; set; } = "";
}