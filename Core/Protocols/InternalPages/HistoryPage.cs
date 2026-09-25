namespace Swifter.Core.Protocols.InternalPages;

public static class HistoryPage
{
    public static string GetHtml()
    {
        return ProtocolPageRenderer.WrapPage("History", $$"""
            {{ProtocolPageRenderer.GetNavHtml()}}
            <h1><span class="icon">📜</span> History</h1>
            <p class="subtitle">Your browsing history</p>
            <input type="text" id="searchBox" class="search-box" placeholder="Search history..." oninput="searchFilter('searchBox','.history-item')">
            <div id="historyList">
                <div style="text-align:center;padding:40px;color:#666;">Loading...</div>
            </div>
            """, """
            .history-item{display:flex;align-items:center;gap:12px;padding:10px 16px;border-bottom:1px solid #2a2a3a;cursor:pointer;transition:background 0.1s;}
            .history-item:hover{background:#2a2a3a;}
            .history-item .url{color:#58a6ff;font-size:12px;white-space:nowrap;overflow:hidden;text-overflow:ellipsis;max-width:400px;}
            .history-item .title{color:#fff;font-size:14px;white-space:nowrap;overflow:hidden;text-overflow:ellipsis;}
            .history-item .time{color:#666;font-size:11px;min-width:120px;text-align:right;}
            .history-item .delete{opacity:0;cursor:pointer;color:#f85149;font-size:14px;transition:opacity 0.15s;}
            .history-item:hover .delete{opacity:1;}
            """, """
            async function loadHistory(){
                try{
                    var response=await fetch('swifter://api/history');
                    var data=await response.json();
                    var list=document.getElementById('historyList');
                    if(data.length===0){list.innerHTML='<div style="text-align:center;padding:40px;color:#666;"><div style="font-size:48px;margin-bottom:16px;">📭</div><p>No history yet</p></div>';return;}
                    list.innerHTML=data.map(function(h){return '<div class="history-item" onclick="window.location.href=\\''+h.url+'\\'"><div style="flex:1;min-width:0;"><div class="title">'+escapeHtml(h.title||h.url)+'</div><div class="url">'+escapeHtml(h.url)+'</div></div><div class="time">'+new Date(h.lastVisited).toLocaleString()+'</div></div>';}).join('');
                }catch(e){document.getElementById('historyList').innerHTML='<p style="color:#888;padding:20px;">History will load from the browser engine.</p>';}
            }
            function escapeHtml(s){var d=document.createElement('div');d.textContent=s;return d.innerHTML;}
            loadHistory();
            """);
    }
}