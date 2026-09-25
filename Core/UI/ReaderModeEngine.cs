namespace Swifter.Core.UI;

public sealed class ReaderModeEngine
{
    private static ReaderModeEngine? _instance;
    private bool _isActive;
    private double _fontSize = 18;
    private string _fontFamily = "Georgia";
    private string _theme = "light";

    public static ReaderModeEngine Instance => _instance ??= new ReaderModeEngine();

    public bool IsActive => _isActive;
    public double FontSize { get => _fontSize; set => _fontSize = Math.Clamp(value, 12, 32); }
    public string FontFamily { get => _fontFamily; set => _fontFamily = value; }
    public string Theme { get => _theme; set => _theme = value; }

    private ReaderModeEngine()
    {
    }

    public string GetActivationScript()
    {
        return $"""
            (function() {{
                if (document.getElementById('swifter-reader-mode')) return;
                var article = document.querySelector('article') || document.querySelector('[role="main"]') || document.querySelector('main') || document.querySelector('.post-content') || document.querySelector('.article-content') || document.querySelector('.entry-content');
                if (!article) article = document.body;
                var overlay = document.createElement('div');
                overlay.id = 'swifter-reader-mode';
                overlay.style.cssText = 'position:fixed;top:0;left:0;width:100%;height:100%;background:{(_theme == "dark" ? "#1a1a2e" : "#fafaf5")};z-index:999999;overflow-y:auto;padding:40px 0;';
                var container = document.createElement('div');
                container.style.cssText = 'max-width:680px;margin:0 auto;padding:0 24px;font-family:{_fontFamily},serif;font-size:{_fontSize}px;line-height:1.8;color:{(_theme == "dark" ? "#d4d4d4" : "#2a2a2a")};';
                var title = document.createElement('h1');
                title.textContent = document.title;
                title.style.cssText = 'font-size:2em;margin-bottom:0.5em;font-weight:700;line-height:1.2;';
                container.appendChild(title);
                var content = article.cloneNode(true);
                var scripts = content.querySelectorAll('script,style,noscript,iframe,nav,footer,header,.ad,.advertisement,.sidebar');
                scripts.forEach(function(el) {{ el.remove(); }});
                var images = content.querySelectorAll('img');
                images.forEach(function(img) {{ img.style.cssText = 'max-width:100%;height:auto;border-radius:8px;margin:16px 0;'; }});
                var links = content.querySelectorAll('a');
                links.forEach(function(a) {{ a.style.color = '{(_theme == "dark" ? "#58a6ff" : "#0066cc")}'; }});
                container.appendChild(content);
                overlay.appendChild(container);
                var closeBtn = document.createElement('button');
                closeBtn.textContent = '✕';
                closeBtn.style.cssText = 'position:fixed;top:16px;right:16px;width:40px;height:40px;border-radius:50%;background:{(_theme == "dark" ? "#333" : "#ddd")};border:none;font-size:18px;cursor:pointer;z-index:1000000;color:{(_theme == "dark" ? "#fff" : "#333")};';
                closeBtn.onclick = function() {{ overlay.remove(); }};
                overlay.appendChild(closeBtn);
                document.body.appendChild(overlay);
            }})();
            """;
    }

    public string GetDeactivationScript()
    {
        return """
            (function() {
                var el = document.getElementById('swifter-reader-mode');
                if (el) el.remove();
            })();
            """;
    }

    public void Activate()
    {
        _isActive = true;
    }

    public void Deactivate()
    {
        _isActive = false;
    }

    public string GetFontSizeScript(double size)
    {
        _fontSize = size;
        return $"""
            (function() {{
                var el = document.getElementById('swifter-reader-mode');
                if (el) {{
                    var container = el.querySelector('div');
                    if (container) container.style.fontSize = '{size}px';
                }}
            }})();
            """;
    }
}