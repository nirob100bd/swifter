using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;

namespace Swifter.Core.Network;

public sealed class WebSocketsDebuggerEngine : IDisposable
{
    private static WebSocketsDebuggerEngine? _instance;
    private readonly ConcurrentDictionary<string, WebSocketConnection> _connections = new();
    private readonly List<WebSocketMessage> _messageLog = new();
    private readonly object _logLock = new();
    private int _maxLogSize = 5000;

    public static WebSocketsDebuggerEngine Instance => _instance ??= new WebSocketsDebuggerEngine();

    public event EventHandler<WebSocketMessage>? MessageReceived;
    public event EventHandler<WebSocketConnection>? ConnectionOpened;
    public event EventHandler<WebSocketConnection>? ConnectionClosed;

    public IReadOnlyList<WebSocketMessage> MessageLog
    {
        get { lock (_logLock) return _messageLog.ToList().AsReadOnly(); }
    }

    public IReadOnlyCollection<WebSocketConnection> ActiveConnections => _connections.Values.ToList().AsReadOnly();

    private WebSocketsDebuggerEngine()
    {
    }

    public async Task<WebSocketConnection?> ConnectAsync(string url, Dictionary<string, string>? headers = null)
    {
        try
        {
            var ws = new ClientWebSocket();
            if (headers != null)
            {
                foreach (var h in headers) ws.Options.SetRequestHeader(h.Key, h.Value);
            }
            await ws.ConnectAsync(new Uri(url), CancellationToken.None);
            var conn = new WebSocketConnection
            {
                Id = Guid.NewGuid().ToString("N"),
                Url = url,
                WebSocket = ws,
                ConnectedAt = DateTime.UtcNow,
                State = WebSocketState.Open
            };
            _connections[conn.Id] = conn;
            ConnectionOpened?.Invoke(this, conn);
            _ = ReceiveLoopAsync(conn);
            return conn;
        }
        catch
        {
            return null;
        }
    }

    private async Task ReceiveLoopAsync(WebSocketConnection conn)
    {
        var buffer = new byte[8192];
        try
        {
            while (conn.WebSocket.State == WebSocketState.Open)
            {
                var result = await conn.WebSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    conn.State = WebSocketState.Closed;
                    ConnectionClosed?.Invoke(this, conn);
                    break;
                }
                var message = new WebSocketMessage
                {
                    ConnectionId = conn.Id,
                    Direction = MessageDirection.Incoming,
                    Data = Encoding.UTF8.GetString(buffer, 0, result.Count),
                    MessageType = result.MessageType == WebSocketMessageType.Text ? "text" : "binary",
                    Timestamp = DateTime.UtcNow
                };
                LogMessage(message);
                MessageReceived?.Invoke(this, message);
            }
        }
        catch
        {
            conn.State = WebSocketState.Closed;
            ConnectionClosed?.Invoke(this, conn);
        }
    }

    public async Task SendAsync(string connectionId, string data)
    {
        if (!_connections.TryGetValue(connectionId, out var conn)) return;
        if (conn.WebSocket.State != WebSocketState.Open) return;
        var bytes = Encoding.UTF8.GetBytes(data);
        await conn.WebSocket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);
        var message = new WebSocketMessage
        {
            ConnectionId = conn.Id,
            Direction = MessageDirection.Outgoing,
            Data = data,
            MessageType = "text",
            Timestamp = DateTime.UtcNow
        };
        LogMessage(message);
    }

    public async Task CloseAsync(string connectionId)
    {
        if (!_connections.TryRemove(connectionId, out var conn)) return;
        if (conn.WebSocket.State == WebSocketState.Open)
        {
            await conn.WebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.Zero);
        }
        conn.State = WebSocketState.Closed;
        conn.WebSocket.Dispose();
        ConnectionClosed?.Invoke(this, conn);
    }

    private void LogMessage(WebSocketMessage message)
    {
        lock (_logLock)
        {
            _messageLog.Add(message);
            while (_messageLog.Count > _maxLogSize) _messageLog.RemoveAt(0);
        }
    }

    public void ClearLog()
    {
        lock (_logLock) _messageLog.Clear();
    }

    public string GenerateHtmlLog()
    {
        List<WebSocketMessage> messages;
        lock (_logLock) messages = _messageLog.ToList();
        return $"""
            <html><head><style>
            body {{ font-family: 'Cascadia Code', monospace; background: #0d1117; color: #c9d1d9; padding: 16px; }}
            .msg {{ padding: 8px 12px; margin: 4px 0; border-radius: 6px; font-size: 13px; }}
            .incoming {{ background: #161b22; border-left: 3px solid #58a6ff; }}
            .outgoing {{ background: #161b22; border-left: 3px solid #3fb950; }}
            .time {{ color: #8b949e; font-size: 11px; }}
            .dir {{ font-weight: bold; margin-right: 8px; }}
            h1 {{ color: #58a6ff; font-size: 20px; }}
            </style></head><body><h1>🔌 WebSocket Debugger</h1>
            {string.Join("", messages.Select(m => $"""
            <div class="msg {(m.Direction == MessageDirection.Incoming ? "incoming" : "outgoing")}">
                <span class="time">{m.Timestamp:HH:mm:ss.fff}</span>
                <span class="dir">{(m.Direction == MessageDirection.Incoming ? "◀" : "▶")}</span>
                {System.Net.WebUtility.HtmlEncode(m.Data.Length > 500 ? m.Data[..500] + "..." : m.Data)}
            </div>
            """))}
            </body></html>
            """;
    }

    public void Dispose()
    {
        foreach (var conn in _connections.Values)
        {
            try { conn.WebSocket.Dispose(); } catch { }
        }
        _connections.Clear();
    }
}

public sealed class WebSocketConnection
{
    public string Id { get; set; } = "";
    public string Url { get; set; } = "";
    public ClientWebSocket WebSocket { get; set; } = null!;
    public DateTime ConnectedAt { get; set; }
    public WebSocketState State { get; set; }
}

public sealed class WebSocketMessage
{
    public string ConnectionId { get; set; } = "";
    public MessageDirection Direction { get; set; }
    public string Data { get; set; } = "";
    public string MessageType { get; set; } = "";
    public DateTime Timestamp { get; set; }
}

public enum MessageDirection
{
    Incoming,
    Outgoing
}