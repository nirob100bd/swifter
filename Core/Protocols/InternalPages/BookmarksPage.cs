namespace Swifter.Core.Protocols.InternalPages;

public static class BookmarksPage
{
    public static string GetHtml()
    {
        return ProtocolPageRenderer.WrapPage("Bookmarks", $$"""
            {{ProtocolPageRenderer.GetNavHtml()}}
            <h1><span class="icon">⭐</span> Bookmarks</h1>
            <p class="subtitle">Your saved bookmarks</p>
            <input type="text" id="searchBox" class="search-box" placeholder="Search bookmarks..." oninput="searchFilter('searchBox','.bookmark-item')">
            <div id="bookmarkList">
                <div style="text-align:center;padding:40px;color:#666;">Loading...</div>
            </div>
            """, """
            .bookmark-item{display:flex;align-items:center;gap:12px;padding:10px 16px;border-bottom:1px solid #2a2a3a;cursor:pointer;transition:background 0.1s;}
            .bookmark-item:hover{background:#2a2a3a;}
            .bookmark-item .folder{background:#333;color:#aaa;padding:2px 8px;border-radius:10px;font-size:11px;}
            .bookmark-item .title{color:#fff;font-size:14px;}
            .bookmark-item .url{color:#58a6ff;font-size:12px;white-space:nowrap;overflow:hidden;text-overflow:ellipsis;}
            """, """
            async function loadBookmarks(){
                try{
                    var response=await fetch('swifter://api/bookmarks');
                    var data=await response.json();
                    var list=document.getElementById('bookmarkList');
                    if(data.length===0){list.innerHTML='<div style="text-align:center;padding:40px;color:#666;"><div style="font-size:48px;margin-bottom:16px;">⭐</div><p>No bookmarks yet</p><p style="font-size:13px;margin-top:8px;">Press Ctrl+D to bookmark a page</p></div>';return;}
                    list.innerHTML=data.map(function(b){return '<div class="bookmark-item" onclick="window.location.href=\\''+b.url+'\\'"><span class="folder">'+escapeHtml(b.folder)+'</span><div style="flex:1;min-width:0;"><div class="title">'+escapeHtml(b.title)+'</div><div class="url">'+escapeHtml(b.url)+'</div></div></div>';}).join('');
                }catch(e){document.getElementById('bookmarkList').innerHTML='<p style="color:#888;padding:20px;">Bookmarks will load from the browser engine.</p>';}
            }
            function escapeHtml(s){var d=document.createElement('div');d.textContent=s;return d.innerHTML;}
            loadBookmarks();
            """);
    }
}