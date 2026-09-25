using System.Net;
using System.Net.NetworkInformation;
using System.IO;

namespace Swifter.Core.Network;

public sealed class VPNProxyTunnelEngine : IDisposable
{
    private static VPNProxyTunnelEngine? _instance;
    private readonly List<ProxyServer> _servers = new();
    private ProxyServer? _activeServer;
    private int _currentIndex;
    private Timer? _rotateTimer;

    public static VPNProxyTunnelEngine Instance => _instance ??= new VPNProxyTunnelEngine();

    public event EventHandler<ProxyServer>? ServerChanged;

    public bool IsConnected => _activeServer != null;
    public ProxyServer? ActiveServer => _activeServer;
    public IReadOnlyList<ProxyServer> Servers => _servers.AsReadOnly();

    private VPNProxyTunnelEngine()
    {
        LoadSavedServers();
    }

    private void LoadSavedServers()
    {
        var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Swifter", "proxies.json");
        if (File.Exists(path))
        {
            try
            {
                var json = File.ReadAllText(path);
                var servers = System.Text.Json.JsonSerializer.Deserialize<List<ProxyServer>>(json);
                if (servers != null) _servers.AddRange(servers);
            }
            catch
            {
            }
        }
    }

    public void AddServer(ProxyServer server)
    {
        _servers.Add(server);
        SaveServers();
    }

    public void RemoveServer(int index)
    {
        if (index >= 0 && index < _servers.Count)
        {
            if (_activeServer == _servers[index]) Disconnect();
            _servers.RemoveAt(index);
            SaveServers();
        }
    }

    public bool Connect(int serverIndex)
    {
        if (serverIndex < 0 || serverIndex >= _servers.Count) return false;
        _activeServer = _servers[serverIndex];
        _currentIndex = serverIndex;
        ServerChanged?.Invoke(this, _activeServer);
        return true;
    }

    public void Disconnect()
    {
        _activeServer = null;
        _rotateTimer?.Dispose();
        _rotateTimer = null;
        ServerChanged?.Invoke(this, null!);
    }

    public void EnableAutoRotation(int intervalSeconds)
    {
        _rotateTimer?.Dispose();
        _rotateTimer = new Timer(_ =>
        {
            if (_servers.Count <= 1) return;
            _currentIndex = (_currentIndex + 1) % _servers.Count;
            Connect(_currentIndex);
        }, null, TimeSpan.FromSeconds(intervalSeconds), TimeSpan.FromSeconds(intervalSeconds));
    }

    public WebProxy? GetWebProxy()
    {
        if (_activeServer == null) return null;
        var proxy = new WebProxy($"{_activeServer.Type.ToString().ToLower()}://{_activeServer.Host}:{_activeServer.Port}");
        if (!string.IsNullOrEmpty(_activeServer.Username))
        {
            proxy.Credentials = new NetworkCredential(_activeServer.Username, _activeServer.Password);
        }
        return proxy;
    }

    private void SaveServers()
    {
        var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Swifter", "proxies.json");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var json = System.Text.Json.JsonSerializer.Serialize(_servers, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(path, json);
    }

    public void Dispose()
    {
        _rotateTimer?.Dispose();
    }
}

public sealed class ProxyServer
{
    public string Name { get; set; } = "";
    public string Host { get; set; } = "";
    public int Port { get; set; }
    public ProxyType Type { get; set; } = ProxyType.SOCKS5;
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
    public string Country { get; set; } = "";
}

public enum ProxyType
{
    HTTP,
    SOCKS4,
    SOCKS5
}