namespace Swifter.Core.Protocols.InternalPages;

public static class NotesPage
{
    public static string GetHtml()
    {
        return ProtocolPageRenderer.WrapPage("Notes", $$"""
            {{ProtocolPageRenderer.GetNavHtml()}}
            <h1><span class="icon">📝</span> Notes</h1>
            <p class="subtitle">Your web clips and quick notes</p>
            <input type="text" id="searchBox" class="search-box" placeholder="Search notes..." oninput="searchFilter('searchBox','.note-card')">
            <div id="notesList">
                <div class="card" style="text-align:center;padding:40px;">
                    <div style="font-size:48px;margin-bottom:16px;">📝</div>
                    <p style="color:#888;">No notes yet</p>
                    <p style="color:#666;font-size:13px;margin-top:8px;">Use the floating notes overlay or clip web content to create notes</p>
                </div>
            </div>
            """, """
            .note-card{background:#22223a;border-radius:10px;padding:16px;margin:8px 0;border-left:3px solid #FFD700;cursor:pointer;transition:background 0.15s;}
            .note-card:hover{background:#2a2a42;}
            .note-title{font-size:15px;font-weight:600;color:#fff;}
            .note-meta{font-size:11px;color:#666;margin-top:4px;}
            .note-preview{font-size:13px;color:#aaa;margin-top:8px;display:-webkit-box;-webkit-line-clamp:2;-webkit-box-orient:vertical;overflow:hidden;}
            """);
    }
}