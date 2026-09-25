namespace Swifter.Core.Protocols.InternalPages;

public static class TorrentPage
{
    public static string GetHtml()
    {
        return ProtocolPageRenderer.WrapPage("Torrent Streamer", $"""
            {ProtocolPageRenderer.GetNavHtml()}
            <h1><span class="icon">🌊</span> Torrent Streamer</h1>
            <p class="subtitle">Stream media from torrents directly in the browser</p>
            <div style="display:flex;gap:8px;margin-bottom:16px;">
                <input type="text" id="torrentUrl" placeholder="Enter magnet link or .torrent URL..." style="flex:1;">
                <button class="btn btn-primary" onclick="startStream()">Start Stream</button>
            </div>
            <div class="card" style="background:#1a2a3a;border:1px solid #2a4a5a;">
                <div style="display:flex;align-items:center;gap:12px;">
                    <div style="font-size:28px;">⚡</div>
                    <div>
                        <div style="color:#58a6ff;font-weight:600;">P2P Streaming</div>
                        <div style="color:#888;font-size:13px;">Requires webtorrent-cli installed globally</div>
                    </div>
                </div>
            </div>
            <div class="divider"></div>
            <h2 style="color:#fff;font-size:18px;margin-bottom:12px;">How to Use</h2>
            <div class="card">
                <ol style="color:#aaa;padding-left:20px;line-height:2;">
                    <li>Install webtorrent: <code style="background:#333;padding:2px 6px;border-radius:4px;">npm install -g webtorrent-cli</code></li>
                    <li>Paste a magnet link or .torrent URL above</li>
                    <li>Click "Start Stream" to begin</li>
                    <li>The video will play directly in the browser tab</li>
                </ol>
            </div>
            """);
    }
}