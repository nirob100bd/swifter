namespace Swifter.Core.UI;

public sealed class TabSnoozeEngine : IDisposable
{
    private static TabSnoozeEngine? _instance;
    private readonly List<SnoozedTab> _snoozedTabs = new();
    private Timer? _checkTimer;

    public static TabSnoozeEngine Instance => _instance ??= new TabSnoozeEngine();

    public event EventHandler<int>? TabSnoozed;
    public event EventHandler<int>? TabAwakened;

    public IReadOnlyList<SnoozedTab> SnoozedTabs => _snoozedTabs.AsReadOnly();

    private TabSnoozeEngine()
    {
        _checkTimer = new Timer(_ => CheckSnoozedTabs(), null, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));
    }

    public void SnoozeTab(int tabId, TimeSpan duration)
    {
        var tab = TabEngine.TabManager.Instance.Tabs.FirstOrDefault(t => t.Id == tabId);
        if (tab == null) return;
        _snoozedTabs.Add(new SnoozedTab
        {
            TabId = tabId,
            Url = tab.Url,
            Title = tab.Title,
            SnoozedAt = DateTime.UtcNow,
            WakeAt = DateTime.UtcNow.Add(duration)
        });
        TabEngine.TabManager.Instance.CloseTab(tabId);
        TabSnoozed?.Invoke(this, tabId);
    }

    public void SnoozeTab(int tabId, SnoozePreset preset)
    {
        var duration = preset switch
        {
            SnoozePreset.FifteenMinutes => TimeSpan.FromMinutes(15),
            SnoozePreset.OneHour => TimeSpan.FromHours(1),
            SnoozePreset.ThreeHours => TimeSpan.FromHours(3),
            SnoozePreset.Tomorrow => GetTomorrowMorning(),
            SnoozePreset.NextWeek => GetNextMonday(),
            _ => TimeSpan.FromHours(1)
        };
        SnoozeTab(tabId, duration);
    }

    public void CancelSnooze(int tabId)
    {
        _snoozedTabs.RemoveAll(s => s.TabId == tabId);
    }

    private void CheckSnoozedTabs()
    {
        var now = DateTime.UtcNow;
        var toWake = _snoozedTabs.Where(s => s.WakeAt <= now).ToList();
        foreach (var snoozed in toWake)
        {
            _snoozedTabs.Remove(snoozed);
            TabEngine.TabManager.Instance.CreateTab(snoozed.Url, snoozed.Title, false);
            TabAwakened?.Invoke(this, snoozed.TabId);
        }
    }

    private static TimeSpan GetTomorrowMorning()
    {
        var tomorrow = DateTime.Today.AddDays(1).AddHours(9);
        return tomorrow - DateTime.UtcNow;
    }

    private static TimeSpan GetNextMonday()
    {
        var today = DateTime.Today;
        int daysUntilMonday = ((int)DayOfWeek.Monday - (int)today.DayOfWeek + 7) % 7;
        if (daysUntilMonday == 0) daysUntilMonday = 7;
        var nextMonday = today.AddDays(daysUntilMonday).AddHours(9);
        return nextMonday - DateTime.UtcNow;
    }

    public void Dispose()
    {
        _checkTimer?.Dispose();
    }
}

public sealed class SnoozedTab
{
    public int TabId { get; set; }
    public string Url { get; set; } = "";
    public string Title { get; set; } = "";
    public DateTime SnoozedAt { get; set; }
    public DateTime WakeAt { get; set; }
}

public enum SnoozePreset
{
    FifteenMinutes,
    OneHour,
    ThreeHours,
    Tomorrow,
    NextWeek
}