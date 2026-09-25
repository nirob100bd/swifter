using System.Diagnostics;
using System.Management;

namespace Swifter.Core.TabEngine;

public sealed class MemorySaver
{
    private static MemorySaver? _instance;
    private Timer? _monitorTimer;
    private long _memoryThresholdMB = 4096;

    public static MemorySaver Instance => _instance ??= new MemorySaver();

    public event EventHandler<MemoryPressureEventArgs>? MemoryPressureDetected;

    public long MemoryThresholdMB
    {
        get => _memoryThresholdMB;
        set => _memoryThresholdMB = value;
    }

    public long CurrentMemoryMB => Process.GetCurrentProcess().WorkingSet64 / (1024 * 1024);

    private MemorySaver()
    {
    }

    public void StartMonitoring(int intervalSeconds = 30)
    {
        _monitorTimer?.Dispose();
        _monitorTimer = new Timer(_ => CheckMemory(), null, TimeSpan.Zero, TimeSpan.FromSeconds(intervalSeconds));
    }

    public void StopMonitoring()
    {
        _monitorTimer?.Dispose();
        _monitorTimer = null;
    }

    private void CheckMemory()
    {
        var current = CurrentMemoryMB;
        var pressure = (double)current / _memoryThresholdMB;
        if (pressure > 0.85)
        {
            MemoryPressureDetected?.Invoke(this, new MemoryPressureEventArgs
            {
                CurrentMB = current,
                ThresholdMB = _memoryThresholdMB,
                PressureLevel = pressure > 0.95 ? PressureLevel.Critical : PressureLevel.High
            });
        }
    }

    public void TrimWorkingSet()
    {
        try
        {
            var process = Process.GetCurrentProcess();
            process.Refresh();
            GC.Collect(2, GCCollectionMode.Aggressive, true, true);
            GC.WaitForPendingFinalizers();
        }
        catch
        {
        }
    }

    public MemoryStats GetStats()
    {
        var process = Process.GetCurrentProcess();
        process.Refresh();
        return new MemoryStats
        {
            WorkingSetMB = process.WorkingSet64 / (1024 * 1024),
            PrivateMB = process.PrivateMemorySize64 / (1024 * 1024),
            GCHeapMB = GC.GetTotalMemory(false) / (1024 * 1024),
            Gen0Collections = GC.CollectionCount(0),
            Gen1Collections = GC.CollectionCount(1),
            Gen2Collections = GC.CollectionCount(2),
            AvailablePhysicalMB = GetAvailablePhysicalMemoryMB()
        };
    }

    private static long GetAvailablePhysicalMemoryMB()
    {
        try
        {
            using var searcher = new System.Management.ManagementObjectSearcher("SELECT FreePhysicalMemory FROM Win32_OperatingSystem");
            foreach (var obj in searcher.Get())
            {
                return Convert.ToInt64(obj["FreePhysicalMemory"]) / 1024;
            }
        }
        catch
        {
        }
        return 0;
    }

    public void Dispose()
    {
        _monitorTimer?.Dispose();
    }
}

public sealed class MemoryPressureEventArgs : EventArgs
{
    public long CurrentMB { get; set; }
    public long ThresholdMB { get; set; }
    public PressureLevel PressureLevel { get; set; }
}

public sealed class MemoryStats
{
    public long WorkingSetMB { get; set; }
    public long PrivateMB { get; set; }
    public long GCHeapMB { get; set; }
    public int Gen0Collections { get; set; }
    public int Gen1Collections { get; set; }
    public int Gen2Collections { get; set; }
    public long AvailablePhysicalMB { get; set; }
}

public enum PressureLevel
{
    Normal,
    High,
    Critical
}