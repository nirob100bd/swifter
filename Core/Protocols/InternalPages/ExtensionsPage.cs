namespace Swifter.Core.Protocols.InternalPages;

public static class ExtensionsPage
{
    public static string GetHtml()
    {
        return ProtocolPageRenderer.WrapPage("Extensions", $$"""
            {{ProtocolPageRenderer.GetNavHtml()}}
            <h1><span class="icon">🧩</span> Extensions</h1>
            <p class="subtitle">Manage browser extensions</p>
            <div class="card" style="text-align:center;padding:40px;">
                <div style="font-size:48px;margin-bottom:16px;">🧩</div>
                <p style="color:#888;">No extensions installed</p>
                <p style="color:#666;font-size:13px;margin-top:8px;">Extensions are loaded from the Swifter/Extensions directory</p>
                <p style="color:#555;font-size:12px;margin-top:16px;">Path: %APPDATA%/Swifter/Extensions/</p>
            </div>
            <div class="divider"></div>
            <h2 style="color:#fff;font-size:18px;margin-bottom:12px;">How to Install</h2>
            <div class="card">
                <ol style="color:#aaa;padding-left:20px;line-height:2;">
                    <li>Create a folder in the Extensions directory</li>
                    <li>Add a <code style="background:#333;padding:2px 6px;border-radius:4px;">manifest.json</code> file with name, version, and description</li>
                    <li>Add JavaScript files for content scripts and background scripts</li>
                    <li>Restart the browser to load extensions</li>
                </ol>
            </div>
            """);
    }
}