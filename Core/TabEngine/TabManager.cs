using System.Collections.ObjectModel;

namespace Swifter.Core.TabEngine;

public sealed class TabManager
{
    private static TabManager? _instance;
    private readonly ObservableCollection<TabData> _tabs = new();
    private int _nextId = 1;

    public static TabManager Instance => _instance ??= new TabManager();

    public ObservableCollection<TabData> Tabs => _tabs;
    public TabData? ActiveTab { get; private set; }

    public event EventHandler<TabData>? TabCreated;
    public event EventHandler<TabData>? TabClosed;
    public event EventHandler<TabData>? ActiveTabChanged;
    public event EventHandler<TabData>? TabNavigated;

    public TabData CreateTab(string url = "swifter://newtab", string title = "New Tab", bool activate = true)
    {
        var tab = new TabData
        {
            Id = _nextId++,
            Url = url,
            Title = title,
            CreatedAt = DateTime.UtcNow,
            IsActive = false
        };
        _tabs.Add(tab);
        if (activate) SetActive(tab.Id);
        TabCreated?.Invoke(this, tab);
        return tab;
    }

    public void CloseTab(int tabId)
    {
        var tab = _tabs.FirstOrDefault(t => t.Id == tabId);
        if (tab == null) return;
        var index = _tabs.IndexOf(tab);
        _tabs.Remove(tab);
        if (ActiveTab == tab)
        {
            if (_tabs.Count > 0)
            {
                var next = _tabs[Math.Min(index, _tabs.Count - 1)];
                SetActive(next.Id);
            }
            else
            {
                ActiveTab = null;
            }
        }
        TabClosed?.Invoke(this, tab);
    }

    public void SetActive(int tabId)
    {
        if (ActiveTab != null) ActiveTab.IsActive = false;
        var tab = _tabs.FirstOrDefault(t => t.Id == tabId);
        if (tab == null) return;
        tab.IsActive = true;
        ActiveTab = tab;
        ActiveTabChanged?.Invoke(this, tab);
    }

    public void NavigateTab(int tabId, string url, string title = "")
    {
        var tab = _tabs.FirstOrDefault(t => t.Id == tabId);
        if (tab == null) return;
        tab.Url = url;
        tab.Title = string.IsNullOrEmpty(title) ? url : title;
        tab.LastNavigated = DateTime.UtcNow;
        TabNavigated?.Invoke(this, tab);
    }

    public void UpdateTitle(int tabId, string title)
    {
        var tab = _tabs.FirstOrDefault(t => t.Id == tabId);
        if (tab != null) tab.Title = title;
    }

    public void UpdateFavicon(int tabId, string faviconUrl)
    {
        var tab = _tabs.FirstOrDefault(t => t.Id == tabId);
        if (tab != null) tab.FaviconUrl = faviconUrl;
    }

    public void DuplicateTab(int tabId)
    {
        var tab = _tabs.FirstOrDefault(t => t.Id == tabId);
        if (tab == null) return;
        CreateTab(tab.Url, tab.Title, true);
    }

    public void MoveTab(int fromIndex, int toIndex)
    {
        if (fromIndex < 0 || fromIndex >= _tabs.Count || toIndex < 0 || toIndex >= _tabs.Count) return;
        var tab = _tabs[fromIndex];
        _tabs.RemoveAt(fromIndex);
        _tabs.Insert(toIndex, tab);
    }

    public void PinTab(int tabId)
    {
        var tab = _tabs.FirstOrDefault(t => t.Id == tabId);
        if (tab != null) tab.IsPinned = !tab.IsPinned;
    }

    public List<TabData> GetTabSnapshot()
    {
        return _tabs.Select(t => new TabData
        {
            Id = t.Id,
            Url = t.Url,
            Title = t.Title,
            IsActive = t.IsActive,
            IsPinned = t.IsPinned,
            FaviconUrl = t.FaviconUrl,
            CreatedAt = t.CreatedAt
        }).ToList();
    }

    public void RestoreTabs(List<TabData> snapshot)
    {
        _tabs.Clear();
        foreach (var tab in snapshot)
        {
            _tabs.Add(tab);
            _nextId = Math.Max(_nextId, tab.Id + 1);
        }
        var active = snapshot.FirstOrDefault(t => t.IsActive);
        if (active != null) SetActive(active.Id);
    }
}

public sealed class TabData
{
    public int Id { get; set; }
    public string Url { get; set; } = "";
    public string Title { get; set; } = "New Tab";
    public string? FaviconUrl { get; set; }
    public bool IsActive { get; set; }
    public bool IsPinned { get; set; }
    public bool IsLoading { get; set; }
    public bool IsHibernate { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime LastNavigated { get; set; }
    public DateTime LastActive { get; set; }
    public long MemoryUsage { get; set; }
    public int GroupId { get; set; } = -1;
    public string GroupColor { get; set; } = "";
}