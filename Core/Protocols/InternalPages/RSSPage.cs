namespace Swifter.Core.Protocols.InternalPages;

public static class RSSPage
{
    public static string GetHtml()
    {
        return ProtocolPageRenderer.WrapPage("RSS Feeds", $"""
            {ProtocolPageRenderer.GetNavHtml()}
            <h1><span class="icon">📰</span> RSS Feeds</h1>
            <p class="subtitle">Stay updated with your favorite content</p>
            <div style="display:flex;gap:8px;margin-bottom:16px;">
                <input type="text" id="feedUrl" placeholder="Enter RSS feed URL..." style="flex:1;">
                <button class="btn btn-primary" onclick="addFeed()">Add Feed</button>
            </div>
            <div class="divider"></div>
            <h2 style="color:#fff;font-size:18px;margin-bottom:12px;">Your Feeds</h2>
            <div id="feedsList">
                <div class="card" style="text-align:center;padding:40px;">
                    <div style="font-size:48px;margin-bottom:16px;">📰</div>
                    <p style="color:#888;">No RSS feeds added</p>
                    <p style="color:#666;font-size:13px;margin-top:8px;">Add an RSS feed URL above to start reading</p>
                </div>
            </div>
            <div class="divider"></div>
            <h2 style="color:#fff;font-size:18px;margin-bottom:12px;">Latest Articles</h2>
            <div id="articlesList">
                <p style="color:#666;padding:20px;">Articles from your feeds will appear here.</p>
            </div>
            """, """
            .feed-item{display:flex;align-items:center;gap:12px;padding:10px 16px;border-bottom:1px solid #2a2a3a;}
            .feed-name{color:#fff;font-size:14px;font-weight:500;}
            .feed-url{color:#58a6ff;font-size:12px;}
            .article-item{padding:12px 16px;border-bottom:1px solid #2a2a3a;cursor:pointer;transition:background 0.1s;}
            .article-item:hover{background:#2a2a3a;}
            .article-title{color:#fff;font-size:14px;font-weight:500;}
            .article-meta{color:#666;font-size:11px;margin-top:4px;}
            """);
    }
}