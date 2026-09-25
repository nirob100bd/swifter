#pragma warning disable SYSLIB0014
using System.IO;
using System.Net;

namespace Swifter.Core.Network;

public sealed class FTPClientEngine : IDisposable
{
    private static FTPClientEngine? _instance;
    private FtpState? _currentState;

    public static FTPClientEngine Instance => _instance ??= new FTPClientEngine();

    public FtpState? CurrentState => _currentState;
    public bool IsConnected => _currentState?.IsConnected == true;

    private FTPClientEngine()
    {
    }

    public async Task<bool> ConnectAsync(string host, int port, string username, string password, bool useSftp = false)
    {
        try
        {
            _currentState = new FtpState
            {
                Host = host,
                Port = port,
                Username = username,
                IsConnected = true,
                UseSftp = useSftp,
                CurrentPath = "/"
            };
            await Task.Delay(100);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public void Disconnect()
    {
        _currentState = null;
    }

    public async Task<List<FtpEntry>> ListDirectoryAsync(string path)
    {
        if (!IsConnected || _currentState == null) return new List<FtpEntry>();
        try
        {
            var uri = $"ftp://{_currentState.Host}:{_currentState.Port}{path}";
            var request = (FtpWebRequest)WebRequest.Create(uri);
            request.Method = WebRequestMethods.Ftp.ListDirectoryDetails;
            request.Credentials = new NetworkCredential(_currentState.Username, "");
            using var response = (FtpWebResponse)await request.GetResponseAsync();
            using var reader = new StreamReader(response.GetResponseStream());
            var content = await reader.ReadToEndAsync();
            return ParseFtpListing(content, path);
        }
        catch
        {
            return new List<FtpEntry>();
        }
    }

    private static List<FtpEntry> ParseFtpListing(string listing, string basePath)
    {
        var entries = new List<FtpEntry>();
        foreach (var line in listing.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = line.Split(new[] { ' ' }, 9, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 9) continue;
            entries.Add(new FtpEntry
            {
                Permissions = parts[0],
                Size = long.TryParse(parts[4], out var size) ? size : 0,
                ModifiedDate = $"{parts[5]} {parts[6]} {parts[7]}",
                Name = parts[8],
                IsDirectory = parts[0].StartsWith("d"),
                FullPath = Path.Combine(basePath, parts[8]).Replace('\\', '/')
            });
        }
        return entries;
    }

    public async Task<byte[]> DownloadFileAsync(string path)
    {
        if (!IsConnected || _currentState == null) return Array.Empty<byte>();
        try
        {
            var uri = $"ftp://{_currentState.Host}:{_currentState.Port}{path}";
            var request = (FtpWebRequest)WebRequest.Create(uri);
            request.Method = WebRequestMethods.Ftp.DownloadFile;
            request.Credentials = new NetworkCredential(_currentState.Username, "");
            using var response = (FtpWebResponse)await request.GetResponseAsync();
            using var ms = new MemoryStream();
            await response.GetResponseStream().CopyToAsync(ms);
            return ms.ToArray();
        }
        catch
        {
            return Array.Empty<byte>();
        }
    }

    public async Task<bool> UploadFileAsync(string path, byte[] data)
    {
        if (!IsConnected || _currentState == null) return false;
        try
        {
            var uri = $"ftp://{_currentState.Host}:{_currentState.Port}{path}";
            var request = (FtpWebRequest)WebRequest.Create(uri);
            request.Method = WebRequestMethods.Ftp.UploadFile;
            request.Credentials = new NetworkCredential(_currentState.Username, "");
            request.ContentLength = data.Length;
            using var stream = await request.GetRequestStreamAsync();
            await stream.WriteAsync(data);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> DeleteFileAsync(string path)
    {
        if (!IsConnected || _currentState == null) return false;
        try
        {
            var uri = $"ftp://{_currentState.Host}:{_currentState.Port}{path}";
            var request = (FtpWebRequest)WebRequest.Create(uri);
            request.Method = WebRequestMethods.Ftp.DeleteFile;
            request.Credentials = new NetworkCredential(_currentState.Username, "");
            using var response = (FtpWebResponse)await request.GetResponseAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public string GenerateHtmlListing(List<FtpEntry> entries)
    {
        var rows = string.Join("", entries.Select(e => $"""
            <div class="entry" onclick="window.location.href='{e.FullPath}'">
                <span class="icon">{(e.IsDirectory ? "📁" : "📄")}</span>
                <span class="name">{e.Name}</span>
                <span class="size">{(e.IsDirectory ? "-" : FormatSize(e.Size))}</span>
                <span class="date">{e.ModifiedDate}</span>
            </div>
            """));
        return $$"""
            <html><head><style>
            body { font-family: 'Segoe UI', sans-serif; background: #1a1a2e; color: #e0e0e0; padding: 20px; }
            .entry { padding: 10px 16px; border-bottom: 1px solid #2a2a3e; display: flex; align-items: center; cursor: pointer; }
            .entry:hover { background: #2a2a3e; }
            .icon { margin-right: 12px; font-size: 18px; }
            .name { flex: 1; }
            .size { color: #888; min-width: 100px; text-align: right; }
            .date { color: #666; min-width: 160px; text-align: right; }
            h1 { color: #0078d4; font-size: 22px; }
            </style></head><body><h1>📂 FTP Browser</h1>
            {{rows}}
            </body></html>
            """;
    }

    private static string FormatSize(long bytes) => bytes switch
    {
        < 1024 => $"{bytes} B",
        < 1024 * 1024 => $"{bytes / 1024.0:F1} KB",
        < 1024 * 1024 * 1024 => $"{bytes / (1024.0 * 1024.0):F1} MB",
        _ => $"{bytes / (1024.0 * 1024.0 * 1024.0):F2} GB"
    };

    public void Dispose()
    {
        Disconnect();
    }
}

public sealed class FtpState
{
    public string Host { get; set; } = "";
    public int Port { get; set; }
    public string Username { get; set; } = "";
    public bool IsConnected { get; set; }
    public bool UseSftp { get; set; }
    public string CurrentPath { get; set; } = "/";
}

public sealed class FtpEntry
{
    public string Permissions { get; set; } = "";
    public long Size { get; set; }
    public string ModifiedDate { get; set; } = "";
    public string Name { get; set; } = "";
    public bool IsDirectory { get; set; }
    public string FullPath { get; set; } = "";
}
#pragma warning restore SYSLIB0014
