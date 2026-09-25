namespace Swifter.Core.Protocols.InternalPages;

public static class DownloadsPage
{
    public static string GetHtml()
    {
        var downloads = Network.DownloadQueueManager.Instance;
        var active = downloads.ActiveDownloads;
        return ProtocolPageRenderer.WrapPage("Downloads", $"""
            {ProtocolPageRenderer.GetNavHtml()}
            <h1><span class="icon">📥</span> Downloads</h1>
            <p class="subtitle">Manage your downloads</p>
            <div class="divider"></div>
            {(active.Count == 0 ? """
                <div style="text-align:center;padding:60px 0;color:#666;">
                    <div style="font-size:48px;margin-bottom:16px;">📭</div>
                    <p>No active downloads</p>
                    <p style="font-size:13px;margin-top:8px;">Files you download will appear here</p>
                </div>
            """ : string.Join("", active.Select(d => $"""
                <div class="card" style="display:flex;align-items:center;gap:16px;">
                    <div style="font-size:24px;">{(d.Status == Network.DownloadStatus.Downloading ? "⬇️" : d.Status == Network.DownloadStatus.Completed ? "✅" : "❌")}</div>
                    <div style="flex:1;">
                        <div style="color:#fff;font-weight:500;">{System.Net.WebUtility.HtmlEncode(d.FileName)}</div>
                        <div style="font-size:12px;color:#888;margin-top:4px;">{FormatSize(d.BytesDownloaded)} / {FormatSize(d.TotalBytes)} • {d.ProgressPercent:F1}%</div>
                        <div style="height:4px;background:#333;border-radius:2px;margin-top:6px;overflow:hidden;">
                            <div style="height:100%;width:{d.ProgressPercent:F1}%;background:#0078d4;border-radius:2px;transition:width 0.3s;"></div>
                        </div>
                    </div>
                    <span class="badge {(d.Status == Network.DownloadStatus.Completed ? "badge-green" : d.Status == Network.DownloadStatus.Failed ? "badge-red" : "badge-blue")}">{d.Status}</span>
                </div>
            """)))}
            """, """
            .download-progress{height:4px;background:#333;border-radius:2px;margin-top:6px;overflow:hidden;}
            .download-bar{height:100%;background:#0078d4;border-radius:2px;transition:width 0.3s;}
            """);
    }

    private static string FormatSize(long bytes) => bytes switch
    {
        < 1024 => $"{bytes} B",
        < 1048576 => $"{bytes / 1024.0:F1} KB",
        < 1073741824 => $"{bytes / 1048576.0:F1} MB",
        _ => $"{bytes / 1073741824.0:F2} GB"
    };
}