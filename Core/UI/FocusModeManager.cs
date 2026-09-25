namespace Swifter.Core.UI;

public sealed class FocusModeManager
{
    private static FocusModeManager? _instance;
    private bool _isActive;
    private FocusLevel _level = FocusLevel.Standard;

    public static FocusModeManager Instance => _instance ??= new FocusModeManager();

    public bool IsActive => _isActive;
    public FocusLevel Level => _level;

    public event EventHandler<bool>? FocusModeChanged;

    private FocusModeManager()
    {
    }

    public void Activate(FocusLevel level = FocusLevel.Standard)
    {
        _isActive = true;
        _level = level;
        FocusModeChanged?.Invoke(this, true);
    }

    public void Deactivate()
    {
        _isActive = false;
        FocusModeChanged?.Invoke(this, false);
    }

    public void Toggle(FocusLevel level = FocusLevel.Standard)
    {
        if (_isActive) Deactivate(); else Activate(level);
    }

    public string GetInjectionScript()
    {
        return _level switch
        {
            FocusLevel.Minimal => """
                (function() {
                    var style = document.createElement('style');
                    style.id = 'swifter-focus-minimal';
                    style.textContent = 'header, footer, nav, aside, .sidebar, .ad, .advertisement, .social-share, .related-posts, .comments { display: none !important; } article, main, .post-content, .article-body { max-width: 700px !important; margin: 0 auto !important; }';
                    document.head.appendChild(style);
                })();
                """,
            FocusLevel.Standard => """
                (function() {
                    var style = document.createElement('style');
                    style.id = 'swifter-focus-standard';
                    style.textContent = 'header:not(:first-child), footer, nav, aside, .sidebar, .ad, .advertisement, .social-share, .related-posts, .comments, .popup, .modal:not(.swifter-modal), .cookie-banner, .newsletter-signup, [role="banner"]:not(:first-child), [role="complementary"] { display: none !important; } body { overflow-x: hidden; } article, main { max-width: 700px !important; margin: 0 auto !important; padding: 20px !important; }';
                    document.head.appendChild(style);
                })();
                """,
            FocusLevel.Ultra => """
                (function() {
                    var style = document.createElement('style');
                    style.id = 'swifter-focus-ultra';
                    style.textContent = '* { opacity: 0 !important; transition: opacity 0.3s; } article *, main *, .post-content *, .article-body *, .entry-content * { opacity: 1 !important; } article, main, .post-content, .article-body, .entry-content { max-width: 660px !important; margin: 40px auto !important; padding: 24px !important; opacity: 1 !important; font-size: 18px !important; line-height: 1.8 !important; } body { background: #1a1a2e !important; } img { opacity: 0.9 !important; max-width: 100% !important; }';
                    document.head.appendChild(style);
                })();
                """,
            _ => ""
        };
    }

    public string GetDeactivationScript()
    {
        return """
            (function() {
                var s = document.getElementById('swifter-focus-minimal') || document.getElementById('swifter-focus-standard') || document.getElementById('swifter-focus-ultra');
                if (s) s.remove();
            })();
            """;
    }
}

public enum FocusLevel
{
    Minimal,
    Standard,
    Ultra
}