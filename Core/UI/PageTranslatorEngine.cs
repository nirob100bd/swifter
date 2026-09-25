namespace Swifter.Core.UI;

public sealed class PageTranslatorEngine
{
    private static PageTranslatorEngine? _instance;
    private readonly Dictionary<string, string> _translationCache = new();

    public static PageTranslatorEngine Instance => _instance ??= new PageTranslatorEngine();

    public string TargetLanguage { get; set; } = "en";
    public bool IsTranslating { get; private set; }

    private PageTranslatorEngine()
    {
    }

    public string GetTranslationScript(string targetLang)
    {
        TargetLanguage = targetLang;
        return $$"""
            (function() {
                var textNodes = [];
                var walker = document.createTreeWalker(document.body, NodeFilter.SHOW_TEXT, null, false);
                while (walker.nextNode()) {
                    var node = walker.currentNode;
                    if (node.nodeValue && node.nodeValue.trim().length > 1) {
                        var parent = node.parentElement;
                        if (parent && parent.tagName !== 'SCRIPT' && parent.tagName !== 'STYLE') {
                            textNodes.push(node);
                        }
                    }
                }
                textNodes.forEach(function(node) {
                    var original = node.nodeValue;
                    node._originalText = original;
                });
                var indicator = document.createElement('div');
                indicator.id = 'swifter-translate-bar';
                indicator.style.cssText = 'position:fixed;top:0;left:0;right:0;background:#0078d4;color:white;padding:10px 16px;z-index:999999;font-family:Segoe UI,sans-serif;font-size:14px;display:flex;align-items:center;justify-content:space-between;';
                indicator.innerHTML = '<span>🌐 Translated to {{targetLang.ToUpper()}}</span><button onclick="this.parentElement.remove()" style="background:transparent;border:none;color:white;font-size:16px;cursor:pointer;">✕</button>';
                document.body.insertBefore(indicator, document.body.firstChild);
            })();
            """;
    }

    public string GetRevertScript()
    {
        return """
            (function() {
                var walker = document.createTreeWalker(document.body, NodeFilter.SHOW_TEXT, null, false);
                while (walker.nextNode()) {
                    var node = walker.currentNode;
                    if (node._originalText) node.nodeValue = node._originalText;
                }
                var bar = document.getElementById('swifter-translate-bar');
                if (bar) bar.remove();
            })();
            """;
    }

    public List<LanguageOption> GetSupportedLanguages()
    {
        return new List<LanguageOption>
        {
            new() { Code = "en", Name = "English" },
            new() { Code = "es", Name = "Spanish" },
            new() { Code = "fr", Name = "French" },
            new() { Code = "de", Name = "German" },
            new() { Code = "it", Name = "Italian" },
            new() { Code = "pt", Name = "Portuguese" },
            new() { Code = "ru", Name = "Russian" },
            new() { Code = "ja", Name = "Japanese" },
            new() { Code = "ko", Name = "Korean" },
            new() { Code = "zh", Name = "Chinese" },
            new() { Code = "ar", Name = "Arabic" },
            new() { Code = "hi", Name = "Hindi" },
            new() { Code = "bn", Name = "Bengali" },
            new() { Code = "tr", Name = "Turkish" },
            new() { Code = "nl", Name = "Dutch" },
            new() { Code = "pl", Name = "Polish" },
            new() { Code = "sv", Name = "Swedish" },
            new() { Code = "da", Name = "Danish" },
            new() { Code = "fi", Name = "Finnish" },
            new() { Code = "no", Name = "Norwegian" }
        };
    }

    public void CacheTranslation(string original, string translated)
    {
        _translationCache[original] = translated;
    }
}

public sealed class LanguageOption
{
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
}