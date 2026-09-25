namespace Swifter.Core.Protocols.InternalPages;

public static class WorkspacesPage
{
    public static string GetHtml()
    {
        var workspaces = UI.WorkspaceManager.Instance;
        var cardsHtml = string.Join("", workspaces.Workspaces.Select((ws, i) => $"""
            <div class="card" style="cursor:pointer;border-left:3px solid {ws.Color};">
                <div style="font-size:28px;margin-bottom:8px;">{ws.Icon}</div>
                <div style="color:#fff;font-size:16px;font-weight:600;">{ws.Name}</div>
                <div style="color:#666;font-size:12px;margin-top:4px;">{(ws.SavedTabs?.Count ?? 0)} tabs saved</div>
            </div>
            """));
        return ProtocolPageRenderer.WrapPage("Workspaces", $$"""
            {{ProtocolPageRenderer.GetNavHtml()}}
            <h1><span class="icon">🗂️</span> Workspaces</h1>
            <p class="subtitle">Organize your tabs into workspaces</p>
            <div class="grid grid-2">
            {{cardsHtml}}
            </div>
            """);
    }
}