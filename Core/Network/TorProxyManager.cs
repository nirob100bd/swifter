using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.IO;

namespace Swifter.Core.Network;

public sealed class TorProxyManager : IDisposable
{
    private static TorProxyManager? _instance;
    private Process? _torProcess;
    private bool _isRunning;
    private string _socksPort = "9050";
    private string _controlPort = "9051";

    public static TorProxyManager Instance => _instance ??= new TorProxyManager();

    public bool IsRunning => _isRunning;
    public string SocksPort => _socksPort;
    public WebProxy? Proxy { get; private set; }

    public event EventHandler<bool>? StatusChanged;

    private TorProxyManager()
    {
    }

    public async Task<bool> StartAsync(string torPath = "tor")
    {
        if (_isRunning) return true;
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = torPath,
                Arguments = $"--SocksPort {_socksPort} --ControlPort {_controlPort} --DataDirectory \"{Path.Combine(Path.GetTempPath(), "swifter_tor")}\"",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            _torProcess = Process.Start(startInfo);
            if (_torProcess == null) return false;

            await Task.Delay(3000);
            if (_torProcess.HasExited) return false;

            _isRunning = true;
            Proxy = new WebProxy($"socks5://127.0.0.1:{_socksPort}");
            StatusChanged?.Invoke(this, true);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public void Stop()
    {
        if (!_isRunning) return;
        try
        {
            _torProcess?.Kill();
            _torProcess?.Dispose();
        }
        catch
        {
        }
        _torProcess = null;
        _isRunning = false;
        Proxy = null;
        StatusChanged?.Invoke(this, false);
    }

    public async Task<bool> RequestNewCircuitAsync()
    {
        if (!_isRunning) return false;
        try
        {
            using var client = new HttpClient();
            client.Timeout = TimeSpan.FromSeconds(10);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public void Dispose()
    {
        Stop();
    }
}