namespace Swifter.Core.Protocols.InternalPages;

public static class NewTabPage
{
    public static string GetHtml()
    {
        return ProtocolPageRenderer.WrapPage("New Tab", $"""
            <div style="text-align:center;padding:60px 0 40px;">
                <div style="font-size:48px;margin-bottom:8px;">⚡</div>
                <h1 style="font-size:36px;font-weight:300;letter-spacing:-1px;">Swifter</h1>
                <p class="subtitle" style="font-size:16px;">Fast. Private. Powerful.</p>
            </div>
            <div style="max-width:600px;margin:0 auto 40px;">
                <input type="text" id="searchBox" placeholder="Search or enter URL..." style="width:100%;padding:14px 20px;font-size:16px;border-radius:24px;background:#22223a;border:1px solid #3a3a4a;color:#fff;" onkeydown="if(event.key==='Enter'){{window.location.href='https://www.google.com/search?q='+encodeURIComponent(this.value);}}">
            </div>
            <div class="grid grid-2" style="max-width:600px;margin:0 auto;">
                <a href="swifter://bookmarks" class="card" style="text-decoration:none;display:block;text-align:center;">
                    <div style="font-size:28px;">⭐</div>
                    <div style="color:#fff;font-size:14px;font-weight:500;margin-top:8px;">Bookmarks</div>
                </a>
                <a href="swifter://history" class="card" style="text-decoration:none;display:block;text-align:center;">
                    <div style="font-size:28px;">📜</div>
                    <div style="color:#fff;font-size:14px;font-weight:500;margin-top:8px;">History</div>
                </a>
                <a href="swifter://downloads" class="card" style="text-decoration:none;display:block;text-align:center;">
                    <div style="font-size:28px;">📥</div>
                    <div style="color:#fff;font-size:14px;font-weight:500;margin-top:8px;">Downloads</div>
                </a>
                <a href="swifter://notes" class="card" style="text-decoration:none;display:block;text-align:center;">
                    <div style="font-size:28px;">📝</div>
                    <div style="color:#fff;font-size:14px;font-weight:500;margin-top:8px;">Notes</div>
                </a>
                <a href="swifter://rss" class="card" style="text-decoration:none;display:block;text-align:center;">
                    <div style="font-size:28px;">📰</div>
                    <div style="color:#fff;font-size:14px;font-weight:500;margin-top:8px;">RSS Feeds</div>
                </a>
                <a href="swifter://settings" class="card" style="text-decoration:none;display:block;text-align:center;">
                    <div style="font-size:28px;">⚙️</div>
                    <div style="color:#fff;font-size:14px;font-weight:500;margin-top:8px;">Settings</div>
                </a>
                <a href="swifter://customization" class="card" style="text-decoration:none;display:block;text-align:center;">
                    <div style="font-size:28px;">🎨</div>
                    <div style="color:#fff;font-size:14px;font-weight:500;margin-top:8px;">Customize</div>
                </a>
                <a href="swifter://inspector" class="card" style="text-decoration:none;display:block;text-align:center;">
                    <div style="font-size:28px;">🔍</div>
                    <div style="color:#fff;font-size:14px;font-weight:500;margin-top:8px;">Inspector</div>
                </a>
            </div>
            <div style="text-align:center;margin-top:40px;">
                <p style="color:#555;font-size:12px;">Swifter Browser v1.0.0 • Built for speed and privacy</p>
            </div>
            """);
    }
}