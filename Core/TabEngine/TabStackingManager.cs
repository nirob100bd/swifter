namespace Swifter.Core.TabEngine;

public sealed class TabStackingManager
{
    private static TabStackingManager? _instance;
    private readonly List<TabGroup> _groups = new();
    private int _nextGroupId = 1;

    public static TabStackingManager Instance => _instance ??= new TabStackingManager();

    public IReadOnlyList<TabGroup> Groups => _groups.AsReadOnly();

    public event EventHandler<TabGroup>? GroupCreated;
    public event EventHandler<int>? GroupRemoved;

    private TabStackingManager()
    {
    }

    public TabGroup CreateGroup(string name, string color = "#0078D4")
    {
        var group = new TabGroup
        {
            Id = _nextGroupId++,
            Name = name,
            Color = color,
            CreatedAt = DateTime.UtcNow
        };
        _groups.Add(group);
        GroupCreated?.Invoke(this, group);
        return group;
    }

    public void AddTabToGroup(int tabId, int groupId)
    {
        var group = _groups.FirstOrDefault(g => g.Id == groupId);
        var tab = TabManager.Instance.Tabs.FirstOrDefault(t => t.Id == tabId);
        if (group == null || tab == null) return;
        if (!group.TabIds.Contains(tabId))
        {
            group.TabIds.Add(tabId);
            tab.GroupId = groupId;
            tab.GroupColor = group.Color;
        }
    }

    public void RemoveTabFromGroup(int tabId, int groupId)
    {
        var group = _groups.FirstOrDefault(g => g.Id == groupId);
        if (group == null) return;
        group.TabIds.Remove(tabId);
        var tab = TabManager.Instance.Tabs.FirstOrDefault(t => t.Id == tabId);
        if (tab != null)
        {
            tab.GroupId = -1;
            tab.GroupColor = "";
        }
    }

    public void DissolveGroup(int groupId)
    {
        var group = _groups.FirstOrDefault(g => g.Id == groupId);
        if (group == null) return;
        foreach (var tabId in group.TabIds)
        {
            var tab = TabManager.Instance.Tabs.FirstOrDefault(t => t.Id == tabId);
            if (tab != null)
            {
                tab.GroupId = -1;
                tab.GroupColor = "";
            }
        }
        _groups.Remove(group);
        GroupRemoved?.Invoke(this, groupId);
    }

    public void RenameGroup(int groupId, string newName)
    {
        var group = _groups.FirstOrDefault(g => g.Id == groupId);
        if (group != null) group.Name = newName;
    }

    public void SetGroupColor(int groupId, string color)
    {
        var group = _groups.FirstOrDefault(g => g.Id == groupId);
        if (group == null) return;
        group.Color = color;
        foreach (var tabId in group.TabIds)
        {
            var tab = TabManager.Instance.Tabs.FirstOrDefault(t => t.Id == tabId);
            if (tab != null) tab.GroupColor = color;
        }
    }

    public void SuspendGroup(int groupId)
    {
        var group = _groups.FirstOrDefault(g => g.Id == groupId);
        if (group == null) return;
        foreach (var tabId in group.TabIds)
        {
            TabFreezingEngine.Instance.FreezeTab(tabId);
        }
        group.IsSuspended = true;
    }

    public void ResumeGroup(int groupId)
    {
        var group = _groups.FirstOrDefault(g => g.Id == groupId);
        if (group == null) return;
        foreach (var tabId in group.TabIds)
        {
            TabFreezingEngine.Instance.UnfreezeTab(tabId);
        }
        group.IsSuspended = false;
    }

    public List<TabGroup> GetGroupsForTab(int tabId)
    {
        return _groups.Where(g => g.TabIds.Contains(tabId)).ToList();
    }
}

public sealed class TabGroup
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Color { get; set; } = "#0078D4";
    public List<int> TabIds { get; set; } = new();
    public bool IsSuspended { get; set; }
    public DateTime CreatedAt { get; set; }
}