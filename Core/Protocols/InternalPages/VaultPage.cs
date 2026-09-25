namespace Swifter.Core.Protocols.InternalPages;

public static class VaultPage
{
    public static string GetHtml()
    {
        return ProtocolPageRenderer.WrapPage("Password Vault", $$"""
            {{ProtocolPageRenderer.GetNavHtml()}}
            <h1><span class="icon">🔐</span> Password Vault</h1>
            <p class="subtitle">Encrypted credential storage using Windows DPAPI + AES-256</p>
            <div class="card" style="background:#1a2a1a;border:1px solid #2a4a2a;">
                <div style="display:flex;align-items:center;gap:12px;">
                    <div style="font-size:28px;">🔒</div>
                    <div>
                        <div style="color:#3fb950;font-weight:600;">Vault Encrypted</div>
                        <div style="color:#888;font-size:13px;">All credentials are protected with AES-256 encryption tied to your Windows account</div>
                    </div>
                </div>
            </div>
            <div class="divider"></div>
            <h2 style="color:#fff;font-size:18px;margin-bottom:12px;">Saved Credentials</h2>
            <div id="credentialsList">
                <div class="card"><p style="color:#888;">Saved passwords will appear here when you use the vault.</p></div>
            </div>
            <div class="divider"></div>
            <h2 style="color:#fff;font-size:18px;margin-bottom:12px;">Saved Cards</h2>
            <div class="card"><p style="color:#888;">Credit card information will appear here.</p></div>
            """);
    }
}