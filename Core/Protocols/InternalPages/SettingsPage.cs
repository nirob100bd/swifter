namespace Swifter.Core.Protocols.InternalPages;

public static class SettingsPage
{
    public static string GetHtml()
    {
        var s = Config.SettingsManager.Instance.Settings;
        return ProtocolPageRenderer.WrapPage("Settings", $$"""
            {{ProtocolPageRenderer.GetNavHtml()}}
            <h1><span class="icon">⚙️</span> Settings</h1>
            <p class="subtitle">Configure Swifter to your preferences</p>

            <h2 style="color:#fff;font-size:18px;margin:20px 0 12px;">🌐 General</h2>
            <div class="card">
                <div style="display:grid;gap:16px;">
                    <div><label style="color:#aaa;font-size:13px;display:block;margin-bottom:4px;">Homepage</label><input type="text" value="{{s.General.Homepage}}" style="width:100%;"></div>
                    <div><label style="color:#aaa;font-size:13px;display:block;margin-bottom:4px;">Default Search Engine</label><select style="width:100%;"><option>Google</option><option>Bing</option><option>DuckDuckGo</option><option>Brave Search</option></select></div>
                    <div><label style="display:flex;align-items:center;gap:8px;cursor:pointer;"><input type="checkbox" {{(s.General.RestoreTabsOnStart ? "checked" : "")}}> <span style="color:#ddd;">Restore tabs on startup</span></label></div>
                    <div><label style="display:flex;align-items:center;gap:8px;cursor:pointer;"><input type="checkbox" {{(s.General.ShowBookmarksBar ? "checked" : "")}}> <span style="color:#ddd;">Show bookmarks bar</span></label></div>
                    <div><label style="display:flex;align-items:center;gap:8px;cursor:pointer;"><input type="checkbox" {{(s.General.ShowStatusBar ? "checked" : "")}}> <span style="color:#ddd;">Show status bar</span></label></div>
                </div>
            </div>

            <h2 style="color:#fff;font-size:18px;margin:20px 0 12px;">🔒 Privacy</h2>
            <div class="card">
                <div style="display:grid;gap:12px;">
                    <div><label style="display:flex;align-items:center;gap:8px;cursor:pointer;"><input type="checkbox" {{(s.Privacy.BlockTrackers ? "checked" : "")}}> <span style="color:#ddd;">Block trackers</span></label></div>
                    <div><label style="display:flex;align-items:center;gap:8px;cursor:pointer;"><input type="checkbox" {{(s.Privacy.BlockAds ? "checked" : "")}}> <span style="color:#ddd;">Block advertisements</span></label></div>
                    <div><label style="display:flex;align-items:center;gap:8px;cursor:pointer;"><input type="checkbox" {{(s.Privacy.BlockPopups ? "checked" : "")}}> <span style="color:#ddd;">Block pop-ups</span></label></div>
                    <div><label style="display:flex;align-items:center;gap:8px;cursor:pointer;"><input type="checkbox" {{(s.Privacy.HttpsOnly ? "checked" : "")}}> <span style="color:#ddd;">HTTPS-only mode</span></label></div>
                    <div><label style="display:flex;align-items:center;gap:8px;cursor:pointer;"><input type="checkbox" {{(s.Privacy.DoNotTrack ? "checked" : "")}}> <span style="color:#ddd;">Send Do Not Track header</span></label></div>
                    <div><label style="display:flex;align-items:center;gap:8px;cursor:pointer;"><input type="checkbox" {{(s.Privacy.FingerprintProtection ? "checked" : "")}}> <span style="color:#ddd;">Fingerprint protection</span></label></div>
                    <div><label style="display:flex;align-items:center;gap:8px;cursor:pointer;"><input type="checkbox" {{(s.Privacy.WebRtcLeakProtection ? "checked" : "")}}> <span style="color:#ddd;">WebRTC leak protection</span></label></div>
                </div>
            </div>

            <h2 style="color:#fff;font-size:18px;margin:20px 0 12px;">⚡ Performance</h2>
            <div class="card">
                <div style="display:grid;gap:16px;">
                    <div><label style="display:flex;align-items:center;gap:8px;cursor:pointer;"><input type="checkbox" {{(s.Performance.HardwareAcceleration ? "checked" : "")}}> <span style="color:#ddd;">Hardware acceleration</span></label></div>
                    <div><label style="display:flex;align-items:center;gap:8px;cursor:pointer;"><input type="checkbox" {{(s.Performance.EnableTabHibernation ? "checked" : "")}}> <span style="color:#ddd;">Tab hibernation</span></label></div>
                    <div><label style="color:#aaa;font-size:13px;display:block;margin-bottom:4px;">Tab discard timeout (minutes)</label><input type="number" value="{{s.Performance.TabDiscardTimeoutMin}}" min="5" max="120" style="width:120px;"></div>
                    <div><label style="color:#aaa;font-size:13px;display:block;margin-bottom:4px;">Memory limit (MB)</label><input type="number" value="{{s.Performance.MemoryLimitMB}}" min="512" max="16384" step="256" style="width:120px;"></div>
                    <div><label style="color:#aaa;font-size:13px;display:block;margin-bottom:4px;">Process count</label><input type="number" value="{{s.Performance.ProcessCount}}" min="1" max="16" style="width:120px;"></div>
                </div>
            </div>

            <h2 style="color:#fff;font-size:18px;margin:20px 0 12px;">🌐 Network</h2>
            <div class="card">
                <div style="display:grid;gap:16px;">
                    <div><label style="color:#aaa;font-size:13px;display:block;margin-bottom:4px;">Download threads</label><input type="number" value="{{s.Network.DownloadThreads}}" min="1" max="64" style="width:120px;"></div>
                    <div><label style="color:#aaa;font-size:13px;display:block;margin-bottom:4px;">Max concurrent downloads</label><input type="number" value="{{s.Network.MaxConcurrentDownloads}}" min="1" max="20" style="width:120px;"></div>
                    <div><label style="display:flex;align-items:center;gap:8px;cursor:pointer;"><input type="checkbox" {{(s.Network.TurboMode ? "checked" : "")}}> <span style="color:#ddd;">Turbo mode (aggressive prefetching)</span></label></div>
                    <div><label style="display:flex;align-items:center;gap:8px;cursor:pointer;"><input type="checkbox" {{(s.Network.Preconnect ? "checked" : "")}}> <span style="color:#ddd;">Preconnect to likely destinations</span></label></div>
                </div>
            </div>

            <h2 style="color:#fff;font-size:18px;margin:20px 0 12px;">🔗 DNS</h2>
            <div class="card">
                <div style="display:grid;gap:16px;">
                    <div><label style="display:flex;align-items:center;gap:8px;cursor:pointer;"><input type="checkbox" {{(s.Dns.OverHttps ? "checked" : "")}}> <span style="color:#ddd;">DNS over HTTPS</span></label></div>
                    <div><label style="color:#aaa;font-size:13px;display:block;margin-bottom:4px;">Provider</label><select style="width:100%;"><option>Cloudflare (1.1.1.1)</option><option>Google (8.8.8.8)</option><option>Quad9 (9.9.9.9)</option></select></div>
                </div>
            </div>

            <h2 style="color:#fff;font-size:18px;margin:20px 0 12px;">💾 Downloads</h2>
            <div class="card">
                <div style="display:grid;gap:16px;">
                    <div><label style="color:#aaa;font-size:13px;display:block;margin-bottom:4px;">Download location</label><input type="text" value="{{s.DownloadPath}}" style="width:100%;"></div>
                </div>
            </div>

            <div class="divider"></div>
            <div style="display:flex;gap:8px;">
                <button class="btn btn-primary">Save Settings</button>
                <button class="btn btn-secondary">Reset to Defaults</button>
                <a href="swifter://customization" class="btn btn-secondary">🎨 Customize Theme</a>
            </div>
            """);
    }
}