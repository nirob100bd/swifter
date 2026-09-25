namespace Swifter.Core.TabEngine;

public sealed class HibernateScheduleEngine : IDisposable
{
    private static HibernateScheduleEngine? _instance;
    private Timer? _schedulerTimer;
    private readonly List<HibernateSchedule> _schedules = new();

    public static HibernateScheduleEngine Instance => _instance ??= new HibernateScheduleEngine();

    public event EventHandler<int>? TabScheduled;
    public event EventHandler<int>? TabRevived;

    public IReadOnlyList<HibernateSchedule> Schedules => _schedules.AsReadOnly();

    private HibernateScheduleEngine()
    {
    }

    public void StartScheduler(int checkIntervalSeconds = 30)
    {
        _schedulerTimer?.Dispose();
        _schedulerTimer = new Timer(_ => CheckSchedules(), null, TimeSpan.FromSeconds(checkIntervalSeconds), TimeSpan.FromSeconds(checkIntervalSeconds));
    }

    public void StopScheduler()
    {
        _schedulerTimer?.Dispose();
        _schedulerTimer = null;
    }

    public void ScheduleHibernate(int tabId, TimeSpan delay)
    {
        var schedule = new HibernateSchedule
        {
            TabId = tabId,
            ScheduledAt = DateTime.UtcNow,
            ReviveAt = DateTime.UtcNow.Add(delay),
            IsActive = true
        };
        _schedules.Add(schedule);
        TabFreezingEngine.Instance.FreezeTab(tabId);
        TabScheduled?.Invoke(this, tabId);
    }

    public void ScheduleAutoRevive(int tabId, TimeSpan delay)
    {
        var existing = _schedules.FirstOrDefault(s => s.TabId == tabId);
        if (existing != null)
        {
            existing.ReviveAt = DateTime.UtcNow.Add(delay);
        }
    }

    public void CancelSchedule(int tabId)
    {
        _schedules.RemoveAll(s => s.TabId == tabId);
    }

    public void ReviveNow(int tabId)
    {
        TabFreezingEngine.Instance.UnfreezeTab(tabId);
        _schedules.RemoveAll(s => s.TabId == tabId);
        TabRevived?.Invoke(this, tabId);
    }

    private void CheckSchedules()
    {
        var now = DateTime.UtcNow;
        var toRevive = _schedules.Where(s => s.IsActive && s.ReviveAt <= now).ToList();
        foreach (var schedule in toRevive)
        {
            TabFreezingEngine.Instance.UnfreezeTab(schedule.TabId);
            _schedules.Remove(schedule);
            TabRevived?.Invoke(this, schedule.TabId);
        }
    }

    public void HibernateInactiveTabs(TimeSpan threshold)
    {
        var manager = TabManager.Instance;
        var active = manager.ActiveTab;
        foreach (var tab in manager.Tabs)
        {
            if (tab == active || tab.IsHibernate) continue;
            if (DateTime.UtcNow - tab.LastActive > threshold)
            {
                ScheduleHibernate(tab.Id, TimeSpan.FromHours(2));
            }
        }
    }

    public void Dispose()
    {
        _schedulerTimer?.Dispose();
    }
}

public sealed class HibernateSchedule
{
    public int TabId { get; set; }
    public DateTime ScheduledAt { get; set; }
    public DateTime ReviveAt { get; set; }
    public bool IsActive { get; set; }
}