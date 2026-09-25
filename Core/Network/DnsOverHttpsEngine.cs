using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;

namespace Swifter.Core.Network;

public sealed class DnsOverHttpsEngine : IDisposable
{
    private static DnsOverHttpsEngine? _instance;
    private readonly HttpClient _client;
    private string _provider = "https://cloudflare-dns.com/dns-query";
    private string _fallback = "https://dns.google/dns-query";
    private readonly Dictionary<string, DnsCacheEntry> _cache = new();
    private readonly object _cacheLock = new();
    private bool _enabled;

    public static DnsOverHttpsEngine Instance => _instance ??= new DnsOverHttpsEngine();

    public bool Enabled
    {
        get => _enabled;
        set => _enabled = value;
    }

    public string Provider
    {
        get => _provider;
        set => _provider = value;
    }

    public string Fallback
    {
        get => _fallback;
        set => _fallback = value;
    }

    private DnsOverHttpsEngine()
    {
        var handler = new HttpClientHandler
        {
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
        };
        _client = new HttpClient(handler);
        _client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/dns-json"));
    }

    public async Task<IPAddress[]?> ResolveAsync(string hostname)
    {
        if (!_enabled) return null;

        lock (_cacheLock)
        {
            if (_cache.TryGetValue(hostname, out var cached) && cached.ExpiresAt > DateTime.UtcNow)
            {
                return cached.Addresses;
            }
        }

        try
        {
            var result = await QueryDnsAsync(_provider, hostname);
            if (result == null)
            {
                result = await QueryDnsAsync(_fallback, hostname);
            }
            if (result != null)
            {
                lock (_cacheLock)
                {
                    _cache[hostname] = new DnsCacheEntry
                    {
                        Addresses = result,
                        ExpiresAt = DateTime.UtcNow.AddMinutes(5)
                    };
                }
            }
            return result;
        }
        catch
        {
            return null;
        }
    }

    private async Task<IPAddress[]?> QueryDnsAsync(string endpoint, string hostname)
    {
        var url = $"{endpoint}?name={Uri.EscapeDataString(hostname)}&type=A";
        using var resp = await _client.GetAsync(url);
        if (!resp.IsSuccessStatusCode) return null;
        var json = await resp.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);
        if (!doc.RootElement.TryGetProperty("Answer", out var answers)) return null;
        var ips = new List<IPAddress>();
        foreach (var answer in answers.EnumerateArray())
        {
            if (answer.TryGetProperty("data", out var data))
            {
                if (IPAddress.TryParse(data.GetString(), out var ip))
                {
                    ips.Add(ip);
                }
            }
        }
        return ips.Count > 0 ? ips.ToArray() : null;
    }

    public void ClearCache()
    {
        lock (_cacheLock) _cache.Clear();
    }

    public void Dispose()
    {
        _client.Dispose();
    }

    private sealed class DnsCacheEntry
    {
        public IPAddress[] Addresses { get; set; } = Array.Empty<IPAddress>();
        public DateTime ExpiresAt { get; set; }
    }
}