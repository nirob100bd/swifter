namespace Swifter.Core.Protocols.InternalPages;

public static class CustomizationPage
{
    public static string GetHtml()
    {
        var settings = Config.SettingsManager.Instance.Settings.Appearance;
        return ProtocolPageRenderer.WrapPage("Customize", $"""
            {ProtocolPageRenderer.GetNavHtml()}
            <h1><span class="icon">🎨</span> Customize</h1>
            <p class="subtitle">Make Swifter truly yours</p>
            <div class="grid grid-2">
                <div class="card">
                    <h3 style="color:#fff;font-size:16px;margin-bottom:12px;">Theme</h3>
                    <div style="display:flex;gap:8px;">
                        <div style="width:80px;height:50px;background:#1a1a2e;border-radius:8px;border:2px solid #0078d4;cursor:pointer;" title="Dark"></div>
                        <div style="width:80px;height:50px;background:#f5f5f5;border-radius:8px;border:2px solid transparent;cursor:pointer;" title="Light"></div>
                    </div>
                </div>
                <div class="card">
                    <h3 style="color:#fff;font-size:16px;margin-bottom:12px;">Glass Effect</h3>
                    <select style="width:100%;">
                        <option {(settings.BackdropType == "Mica" ? "selected" : "")}>Mica</option>
                        <option {(settings.BackdropType == "Acrylic" ? "selected" : "")}>Acrylic</option>
                        <option {(settings.BackdropType == "MicaAlt" ? "selected" : "")}>Mica Alt</option>
                    </select>
                </div>
                <div class="card">
                    <h3 style="color:#fff;font-size:16px;margin-bottom:12px;">Accent Color</h3>
                    <div style="display:flex;gap:6px;flex-wrap:wrap;">
                        <div style="width:32px;height:32px;background:#0078d4;border-radius:50%;cursor:pointer;border:2px solid #fff;"></div>
                        <div style="width:32px;height:32px;background:#00994c;border-radius:50%;cursor:pointer;"></div>
                        <div style="width:32px;height:32px;background:#c23;border-radius:50%;cursor:pointer;"></div>
                        <div style="width:32px;height:32px;background:#ff8c00;border-radius:50%;cursor:pointer;"></div>
                        <div style="width:32px;height:32px;background:#882bd6;border-radius:50%;cursor:pointer;"></div>
                        <div style="width:32px;height:32px;background:#00b7c3;border-radius:50%;cursor:pointer;"></div>
                        <div style="width:32px;height:32px;background:#ff4081;border-radius:50%;cursor:pointer;"></div>
                    </div>
                </div>
                <div class="card">
                    <h3 style="color:#fff;font-size:16px;margin-bottom:12px;">Font Size</h3>
                    <input type="range" min="10" max="24" value="{settings.FontSize}" style="width:100%;">
                    <div style="display:flex;justify-content:space-between;font-size:11px;color:#666;margin-top:4px;"><span>Small</span><span>Large</span></div>
                </div>
                <div class="card">
                    <h3 style="color:#fff;font-size:16px;margin-bottom:12px;">Tab Position</h3>
                    <select style="width:100%;">
                        <option {(settings.TabPosition == "Top" ? "selected" : "")}>Top</option>
                        <option {(settings.TabPosition == "Bottom" ? "selected" : "")}>Bottom</option>
                        <option {(settings.TabPosition == "Left" ? "selected" : "")}>Left</option>
                    </select>
                </div>
                <div class="card">
                    <h3 style="color:#fff;font-size:16px;margin-bottom:12px;">Compact Mode</h3>
                    <label style="display:flex;align-items:center;gap:8px;cursor:pointer;">
                        <input type="checkbox" {(settings.CompactMode ? "checked" : "")}> <span style="color:#aaa;">Reduce padding and spacing</span>
                    </label>
                </div>
            </div>
            <div class="divider"></div>
            <button class="btn btn-primary" onclick="alert('Theme saved!')">Save Theme</button>
            """);
    }
}