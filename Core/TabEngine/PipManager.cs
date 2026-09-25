using System.Windows;
using System.Windows.Controls;

namespace Swifter.Core.TabEngine;

public sealed class PipManager
{
    private static PipManager? _instance;
    private readonly Dictionary<int, PipWindowInfo> _pipWindows = new();

    public static PipManager Instance => _instance ??= new PipManager();

    public event EventHandler<PipWindowInfo>? PipCreated;
    public event EventHandler<int>? PipClosed;

    public IReadOnlyDictionary<int, PipWindowInfo> ActivePips => _pipWindows;

    private PipManager()
    {
    }

    public bool CanCreatePip(TabData tab)
    {
        return tab != null && !_pipWindows.ContainsKey(tab.Id) &&
               (tab.Url.Contains("youtube.com") || tab.Url.Contains("twitch.tv") ||
                tab.Url.Contains("vimeo.com") || tab.Url.Contains("tiktok.com") ||
                tab.Url.Contains(".mp4") || tab.Url.Contains(".webm"));
    }

    public PipWindowInfo CreatePip(TabData tab, double width = 480, double height = 270)
    {
        var info = new PipWindowInfo
        {
            TabId = tab.Id,
            Url = tab.Url,
            Title = tab.Title,
            Width = width,
            Height = height,
            X = SystemParameters.WorkArea.Right - width - 20,
            Y = SystemParameters.WorkArea.Bottom - height - 20,
            CreatedAt = DateTime.UtcNow
        };
        _pipWindows[tab.Id] = info;
        PipCreated?.Invoke(this, info);
        return info;
    }

    public void ClosePip(int tabId)
    {
        if (_pipWindows.Remove(tabId))
        {
            PipClosed?.Invoke(this, tabId);
        }
    }

    public void UpdatePosition(int tabId, double x, double y)
    {
        if (_pipWindows.TryGetValue(tabId, out var info))
        {
            info.X = x;
            info.Y = y;
        }
    }

    public void UpdateSize(int tabId, double width, double height)
    {
        if (_pipWindows.TryGetValue(tabId, out var info))
        {
            info.Width = width;
            info.Height = height;
        }
    }

    public void CloseAll()
    {
        var ids = _pipWindows.Keys.ToList();
        _pipWindows.Clear();
        foreach (var id in ids) PipClosed?.Invoke(this, id);
    }
}

public sealed class PipWindowInfo
{
    public int TabId { get; set; }
    public string Url { get; set; } = "";
    public string Title { get; set; } = "";
    public double Width { get; set; }
    public double Height { get; set; }
    public double X { get; set; }
    public double Y { get; set; }
    public DateTime CreatedAt { get; set; }
}