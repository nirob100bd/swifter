namespace Swifter.Core.UI;

public sealed class WebInspectorDebugger
{
    private static WebInspectorDebugger? _instance;
    private readonly List<ConsoleLogEntry> _consoleLog = new();
    private readonly List<NetworkLogEntry> _networkLog = new();
    private readonly object _logLock = new();

    public static WebInspectorDebugger Instance => _instance ??= new WebInspectorDebugger();

    public event EventHandler<ConsoleLogEntry>? ConsoleMessageLogged;

    public IReadOnlyList<ConsoleLogEntry> ConsoleLog { get { lock (_logLock) return _consoleLog.ToList().AsReadOnly(); } }
    public IReadOnlyList<NetworkLogEntry> NetworkLog { get { lock (_logLock) return _networkLog.ToList().AsReadOnly(); } }

    private WebInspectorDebugger()
    {
    }

    public string GetConsoleHookScript()
    {
        return """
            (function() {
                var origLog = console.log, origWarn = console.warn, origError = console.error, origInfo = console.info;
                function send(level, args) {
                    var msg = Array.from(args).map(function(a) { return typeof a === 'object' ? JSON.stringify(a) : String(a); }).join(' ');
                    window.chrome.webview.postMessage({ type: 'console', level: level, message: msg, url: location.href, timestamp: Date.now() });
                }
                console.log = function() { send('log', arguments); origLog.apply(console, arguments); };
                console.warn = function() { send('warn', arguments); origWarn.apply(console, arguments); };
                console.error = function() { send('error', arguments); origError.apply(console, arguments); };
                console.info = function() { send('info', arguments); origInfo.apply(console, arguments); };
                window.addEventListener('error', function(e) { send('error', [e.message + ' at ' + e.filename + ':' + e.lineno]); });
                window.addEventListener('unhandledrejection', function(e) { send('error', ['Unhandled Promise: ' + e.reason]); });
            })();
            """;
    }

    public string GetDomInspectorScript()
    {
        return """
            (function() {
                document.addEventListener('mouseover', function(e) {
                    e.target.style._origOutline = e.target.style.outline;
                    e.target.style.outline = '2px solid #0078d4';
                });
                document.addEventListener('mouseout', function(e) {
                    e.target.style.outline = e.target.style._origOutline || '';
                });
                document.addEventListener('click', function(e) {
                    if (!e.shiftKey) return;
                    e.preventDefault();
                    e.stopPropagation();
                    var el = e.target;
                    var info = { tag: el.tagName, id: el.id, classes: Array.from(el.classList), text: el.textContent.substring(0, 200), attrs: {} };
                    Array.from(el.attributes).forEach(function(a) { info.attrs[a.name] = a.value; });
                    window.chrome.webview.postMessage({ type: 'element', info: info });
                }, true);
            })();
            """;
    }

    public void LogConsoleMessage(string level, string message, string url = "", int line = 0)
    {
        var entry = new ConsoleLogEntry
        {
            Level = level,
            Message = message,
            Url = url,
            Line = line,
            Timestamp = DateTime.UtcNow
        };
        lock (_logLock)
        {
            _consoleLog.Add(entry);
            while (_consoleLog.Count > 10000) _consoleLog.RemoveAt(0);
        }
        ConsoleMessageLogged?.Invoke(this, entry);
    }

    public void LogNetworkRequest(string method, string url, int statusCode, long size, double durationMs)
    {
        lock (_logLock)
        {
            _networkLog.Add(new NetworkLogEntry
            {
                Method = method,
                Url = url,
                StatusCode = statusCode,
                Size = size,
                DurationMs = durationMs,
                Timestamp = DateTime.UtcNow
            });
            while (_networkLog.Count > 5000) _networkLog.RemoveAt(0);
        }
    }

    public void ClearLog()
    {
        lock (_logLock)
        {
            _consoleLog.Clear();
            _networkLog.Clear();
        }
    }

    public string GenerateInspectorPageHtml()
    {
        List<ConsoleLogEntry> logs;
        List<NetworkLogEntry> netLogs;
        lock (_logLock)
        {
            logs = _consoleLog.TakeLast(200).ToList();
            netLogs = _networkLog.TakeLast(100).ToList();
        }
        var consoleHtml = string.Join("", logs.Select(l => $"""
            <div class="log-entry log-{l.Level}"><span style="color:#8b949e">[{l.Timestamp:HH:mm:ss}]</span> [{l.Level.ToUpper()}] {System.Net.WebUtility.HtmlEncode(l.Message)}</div>
            """));
        var netHtml = string.Join("", netLogs.Select(n => $"""
            <div class="net-entry"><span>{n.Method}</span><span style="white-space:nowrap;overflow:hidden;text-overflow:ellipsis">{System.Net.WebUtility.HtmlEncode(n.Url)}</span><span class="status-{n.StatusCode / 100}xx">{n.StatusCode}</span><span>{FormatSize(n.Size)}</span><span>{n.DurationMs:F0}ms</span></div>
            """));
        return $$"""
            <html><head><style>
            body { font-family: 'Cascadia Code', monospace; background: #0d1117; color: #c9d1d9; margin: 0; }
            .tabs { display: flex; background: #161b22; border-bottom: 1px solid #30363d; }
            .tab { padding: 10px 20px; cursor: pointer; color: #8b949e; font-size: 13px; }
            .tab.active { color: #fff; border-bottom: 2px solid #58a6ff; }
            .panel { display: none; padding: 12px; }
            .panel.active { display: block; }
            .log-entry { padding: 4px 8px; border-bottom: 1px solid #21262d; font-size: 12px; }
            .log-error { color: #f85149; }
            .log-warn { color: #d29922; }
            .log-info { color: #58a6ff; }
            .net-entry { display: grid; grid-template-columns: 60px 1fr 60px 80px 80px; padding: 4px 8px; border-bottom: 1px solid #21262d; font-size: 12px; }
            .status-2xx { color: #3fb950; }
            .status-3xx { color: #d29922; }
            .status-4xx { color: #f85149; }
            h2 { color: #58a6ff; font-size: 16px; padding: 12px; margin: 0; }
            </style></head><body>
            <div class="tabs">
                <div class="tab active" onclick="switchTab('console')" data-tab="console">Console ({{logs.Count}})</div>
                <div class="tab" onclick="switchTab('network')" data-tab="network">Network ({{netLogs.Count}})</div>
                <div class="tab" onclick="switchTab('elements')" data-tab="elements">Elements</div>
            </div>
            <div id="console" class="panel active">
            {{consoleHtml}}
            </div>
            <div id="network" class="panel">
            <div class="net-entry" style="font-weight:bold;color:#8b949e"><span>Method</span><span>URL</span><span>Status</span><span>Size</span><span>Time</span></div>
            {{netHtml}}
            </div>
            <div id="elements" class="panel"><p style="color:#8b949e">Hold Shift+Click on any page element to inspect it.</p></div>
            <script>
            function switchTab(id) {
                document.querySelectorAll('.panel').forEach(function(p) { p.classList.remove('active'); });
                document.querySelectorAll('.tab').forEach(function(t) { t.classList.remove('active'); });
                document.getElementById(id).classList.add('active');
                document.querySelector('[data-tab=' + id + ']').classList.add('active');
            }
            </script></body></html>
            """;
    }

    private static string FormatSize(long bytes) => bytes switch
    {
        < 1024 => $"{bytes} B",
        < 1048576 => $"{bytes / 1024.0:F1} KB",
        _ => $"{bytes / 1048576.0:F1} MB"
    };
}

public sealed class ConsoleLogEntry
{
    public string Level { get; set; } = "";
    public string Message { get; set; } = "";
    public string Url { get; set; } = "";
    public int Line { get; set; }
    public DateTime Timestamp { get; set; }
}

public sealed class NetworkLogEntry
{
    public string Method { get; set; } = "";
    public string Url { get; set; } = "";
    public int StatusCode { get; set; }
    public long Size { get; set; }
    public double DurationMs { get; set; }
    public DateTime Timestamp { get; set; }
}