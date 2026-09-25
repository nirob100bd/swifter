namespace Swifter.Core.Protocols;

public static class ProtocolPageRenderer
{
    public static string WrapPage(string title, string bodyHtml, string extraStyles = "", string extraScripts = "")
    {
        return $$"""
            <!DOCTYPE html>
            <html lang="en">
            <head>
            <meta charset="utf-8">
            <meta name="viewport" content="width=device-width,initial-scale=1">
            <title>{{System.Net.WebUtility.HtmlEncode(title)}} - Swifter</title>
            <style>
            *{margin:0;padding:0;box-sizing:border-box;}
            body{font-family:'Segoe UI Variable','Segoe UI',system-ui,-apple-system,sans-serif;background:#1a1a2e;color:#e0e0e0;line-height:1.5;min-height:100vh;}
            .container{max-width:960px;margin:0 auto;padding:32px 24px;}
            h1{font-size:28px;font-weight:600;color:#fff;margin-bottom:8px;}
            h1 .icon{margin-right:8px;}
            .subtitle{font-size:14px;color:#888;margin-bottom:24px;}
            .card{background:#22223a;border-radius:12px;padding:20px;margin:12px 0;transition:background 0.15s;}
            .card:hover{background:#2a2a42;}
            a{color:#0078d4;text-decoration:none;}
            a:hover{text-decoration:underline;}
            .btn{display:inline-block;padding:10px 24px;border-radius:8px;font-size:14px;font-weight:500;border:none;cursor:pointer;transition:opacity 0.15s;}
            .btn:hover{opacity:0.85;}
            .btn-primary{background:#0078d4;color:#fff;}
            .btn-secondary{background:#333;color:#ddd;}
            input,select{background:#1e1e30;border:1px solid #3a3a4a;border-radius:8px;padding:10px 14px;color:#e0e0e0;font-size:14px;outline:none;}
            input:focus,select:focus{border-color:#0078d4;}
            .grid{display:grid;gap:16px;}
            .grid-2{grid-template-columns:repeat(auto-fill,minmax(280px,1fr));}
            .stat{text-align:center;padding:16px;}
            .stat-value{font-size:28px;font-weight:700;color:#fff;}
            .stat-label{font-size:12px;color:#888;margin-top:4px;}
            .badge{display:inline-block;padding:3px 10px;border-radius:20px;font-size:11px;font-weight:600;}
            .badge-green{background:#1a3a1a;color:#3fb950;}
            .badge-red{background:#3a1a1a;color:#f85149;}
            .badge-blue{background:#1a2a3a;color:#58a6ff;}
            .divider{height:1px;background:#2a2a3a;margin:20px 0;}
            .search-box{width:100%;margin-bottom:16px;}
            {{extraStyles}}
            </style>
            </head>
            <body>
            <div class="container">
            {{bodyHtml}}
            </div>
            <script>
            function navigateTo(url){window.location.href=url;}
            function searchFilter(inputId, itemSelector){
                var q=document.getElementById(inputId).value.toLowerCase();
                document.querySelectorAll(itemSelector).forEach(function(el){
                    el.style.display=el.textContent.toLowerCase().indexOf(q)!==-1?'':'none';
                });
            }
            {{extraScripts}}
            </script>
            </body>
            </html>
            """;
    }

    public static string GetNavHtml()
    {
        return """
            <nav style="display:flex;gap:12px;margin-bottom:24px;flex-wrap:wrap;">
            <a href="swifter://newtab" class="btn btn-secondary">🏠 Home</a>
            <a href="swifter://bookmarks" class="btn btn-secondary">⭐ Bookmarks</a>
            <a href="swifter://history" class="btn btn-secondary">📜 History</a>
            <a href="swifter://downloads" class="btn btn-secondary">📥 Downloads</a>
            <a href="swifter://notes" class="btn btn-secondary">📝 Notes</a>
            <a href="swifter://rss" class="btn btn-secondary">📰 RSS</a>
            <a href="swifter://inspector" class="btn btn-secondary">🔍 Inspector</a>
            <a href="swifter://settings" class="btn btn-secondary">⚙️ Settings</a>
            </nav>
            """;
    }
}