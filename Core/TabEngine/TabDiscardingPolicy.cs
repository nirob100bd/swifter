namespace Swifter.Core.TabEngine;

public sealed class TabDiscardingPolicy
{
    private static TabDiscardingPolicy? _instance;
    private DiscardStrategy _strategy = DiscardStrategy.LeastRecentlyUsed;
    private int _maxBackgroundTabs = 50;
    private int _memoryThresholdMB = 4096;

    public static TabDiscardingPolicy Instance => _instance ??= new TabDiscardingPolicy();

    public DiscardStrategy Strategy
    {
        get => _strategy;
        set => _strategy = value;
    }

    public int MaxBackgroundTabs
    {
        get => _maxBackgroundTabs;
        set => _maxBackgroundTabs = value;
    }

    public int MemoryThresholdMB
    {
        get => _memoryThresholdMB;
        set => _memoryThresholdMB = value;
    }

    private TabDiscardingPolicy()
    {
    }

    public List<int> GetTabsToDiscard()
    {
        var manager = TabManager.Instance;
        var tabs = manager.Tabs.Where(t => !t.IsActive && !t.IsHibernate).ToList();
        if (tabs.Count <= _maxBackgroundTabs) return new List<int>();

        var excess = tabs.Count - _maxBackgroundTabs;
        return _strategy switch
        {
            DiscardStrategy.LeastRecentlyUsed => tabs
                .OrderBy(t => t.LastActive)
                .Take(excess)
                .Select(t => t.Id)
                .ToList(),
            DiscardStrategy.OldestFirst => tabs
                .OrderBy(t => t.CreatedAt)
                .Take(excess)
                .Select(t => t.Id)
                .ToList(),
            DiscardStrategy.HighestMemory => tabs
                .OrderByDescending(t => t.MemoryUsage)
                .Take(excess)
                .Select(t => t.Id)
                .ToList(),
            DiscardStrategy.Random => tabs
                .OrderBy(_ => Guid.NewGuid())
                .Take(excess)
                .Select(t => t.Id)
                .ToList(),
            _ => new List<int>()
        };
    }

    public bool ShouldDiscard(TabData tab)
    {
        if (tab.IsActive || tab.IsPinned || tab.IsHibernate) return false;
        var manager = TabManager.Instance;
        var backgroundCount = manager.Tabs.Count(t => !t.IsActive);
        return backgroundCount > _maxBackgroundTabs;
    }

    public void EnforcePolicy()
    {
        var toDiscard = GetTabsToDiscard();
        foreach (var tabId in toDiscard)
        {
            TabFreezingEngine.Instance.FreezeTab(tabId);
        }
    }
}

public enum DiscardStrategy
{
    LeastRecentlyUsed,
    OldestFirst,
    HighestMemory,
    Random
}