namespace Swifter.Core.TabEngine;

public sealed class TabFreezingEngine
{
    private static TabFreezingEngine? _instance;
    private readonly Dictionary<int, FrozenTabState> _frozenTabs = new();
    private Timer? _checkTimer;
    private TimeSpan _inactivityThreshold = TimeSpan.FromMinutes(5);

    public static TabFreezingEngine Instance => _instance ??= new TabFreezingEngine();

    public event EventHandler<int>? TabFrozen;
    public event EventHandler<int>? TabUnfrozen;

    public TimeSpan InactivityThreshold
    {
        get => _inactivityThreshold;
        set => _inactivityThreshold = value;
    }

    public IReadOnlyDictionary<int, FrozenTabState> FrozenTabs => _frozenTabs;

    private TabFreezingEngine()
    {
    }

    public void StartMonitoring(int intervalSeconds = 60)
    {
        _checkTimer?.Dispose();
        _checkTimer = new Timer(_ => CheckInactiveTabs(), null, TimeSpan.FromSeconds(intervalSeconds), TimeSpan.FromSeconds(intervalSeconds));
    }

    public void StopMonitoring()
    {
        _checkTimer?.Dispose();
        _checkTimer = null;
    }

    private void CheckInactiveTabs()
    {
        var manager = TabManager.Instance;
        var active = manager.ActiveTab;
        foreach (var tab in manager.Tabs)
        {
            if (tab == active || tab.IsHibernate || _frozenTabs.ContainsKey(tab.Id)) continue;
            if (DateTime.UtcNow - tab.LastActive > _inactivityThreshold)
            {
                FreezeTab(tab.Id);
            }
        }
    }

    public bool FreezeTab(int tabId)
    {
        if (_frozenTabs.ContainsKey(tabId)) return false;
        var tab = TabManager.Instance.Tabs.FirstOrDefault(t => t.Id == tabId);
        if (tab == null || tab.IsActive) return false;
        _frozenTabs[tabId] = new FrozenTabState
        {
            TabId = tabId,
            FrozenAt = DateTime.UtcNow,
            Url = tab.Url,
            Title = tab.Title
        };
        tab.IsHibernate = true;
        TabFrozen?.Invoke(this, tabId);
        return true;
    }

    public bool UnfreezeTab(int tabId)
    {
        if (!_frozenTabs.Remove(tabId)) return false;
        var tab = TabManager.Instance.Tabs.FirstOrDefault(t => t.Id == tabId);
        if (tab != null)
        {
            tab.IsHibernate = false;
            tab.LastActive = DateTime.UtcNow;
        }
        TabUnfrozen?.Invoke(this, tabId);
        return true;
    }

    public void FreezeAllInactive()
    {
        var manager = TabManager.Instance;
        var active = manager.ActiveTab;
        foreach (var tab in manager.Tabs)
        {
            if (tab != active && !tab.IsActive)
            {
                FreezeTab(tab.Id);
            }
        }
    }

    public void UnfreezeAll()
    {
        var ids = _frozenTabs.Keys.ToList();
        foreach (var id in ids) UnfreezeTab(id);
    }

    public void Dispose()
    {
        _checkTimer?.Dispose();
    }
}

public sealed class FrozenTabState
{
    public int TabId { get; set; }
    public DateTime FrozenAt { get; set; }
    public string Url { get; set; } = "";
    public string Title { get; set; } = "";
}