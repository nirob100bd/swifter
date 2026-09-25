using Microsoft.Web.WebView2.Core;

namespace Swifter.Core.Storage;

public sealed class CookieManager
{
    private static CookieManager? _instance;
    private CoreWebView2CookieManager? _cookieManager;

    public static CookieManager Instance => _instance ??= new CookieManager();

    public void Initialize(CoreWebView2CookieManager manager)
    {
        _cookieManager = manager;
    }

    public List<CoreWebView2Cookie> GetAllCookies()
    {
        if (_cookieManager == null) return new List<CoreWebView2Cookie>();
        return _cookieManager.GetCookies(null).ToList();
    }

    public async Task<List<CoreWebView2Cookie>> GetCookiesForUrlAsync(string url)
    {
        if (_cookieManager == null) return new List<CoreWebView2Cookie>();
        return (await _cookieManager.GetCookiesAsync(url)).ToList();
    }

    public void AddCookie(string name, string value, string domain, string path = "/")
    {
        if (_cookieManager == null) return;
        var cookie = _cookieManager.CreateCookie(name, value, domain, path);
        _cookieManager.AddOrUpdateCookie(cookie);
    }

    public void DeleteCookie(string name, string domain, string path = "/")
    {
        if (_cookieManager == null) return;
        var cookie = _cookieManager.CreateCookie(name, "", domain, path);
        _cookieManager.DeleteCookie(cookie);
    }

    public void ClearAll()
    {
        if (_cookieManager == null) return;
        _cookieManager.DeleteAllCookies();
    }

    public void DeleteCookiesModifiedAfter(DateTime since)
    {
        if (_cookieManager == null) return;
        var cookies = _cookieManager.GetCookies(null).ToList();
        foreach (var cookie in cookies)
        {
            if (cookie.Expires < since)
            {
                _cookieManager.DeleteCookie(cookie);
            }
        }
    }
}