namespace Swifter.Core.Protocols.InternalPages;

public static class ShieldsPage
{
    public static string GetHtml()
    {
        var shield = Network.ShieldEngine.Instance;
        var stats = shield.GetStats();
        var statsHtml = string.Join("", stats.Select(s => $"""
            <div class="card" style="display:flex;justify-content:space-between;align-items:center;">
                <span style="color:#fff;">{s.Key}</span>
                <span class="badge badge-red">{s.Value} blocked</span>
            </div>
            """));
        if (stats.Count == 0)
            statsHtml = """<div class="card"><p style="color:#888;">No items blocked yet</p></div>""";
        return ProtocolPageRenderer.WrapPage("Shields", $$"""
            {{ProtocolPageRenderer.GetNavHtml()}}
            <h1><span class="icon">🛡️</span> Shields</h1>
            <p class="subtitle">Privacy and security controls</p>
            <div class="grid grid-2">
                <div class="card stat">
                    <div class="stat-value" style="color:#3fb950;">{{shield.TotalBlocked}}</div>
                    <div class="stat-label">Total Blocked</div>
                </div>
                <div class="card stat">
                    <div class="stat-value">{{(shield.BlockTrackers ? "ON" : "OFF")}}</div>
                    <div class="stat-label">Tracker Blocking</div>
                </div>
                <div class="card stat">
                    <div class="stat-value">{{(shield.BlockAds ? "ON" : "OFF")}}</div>
                    <div class="stat-label">Ad Blocking</div>
                </div>
                <div class="card stat">
                    <div class="stat-value">{{(shield.BlockPopups ? "ON" : "OFF")}}</div>
                    <div class="stat-label">Popup Blocking</div>
                </div>
            </div>
            <div class="divider"></div>
            <h2 style="color:#fff;font-size:18px;margin-bottom:12px;">Block Statistics</h2>
            {{statsHtml}}
            """);
    }
}