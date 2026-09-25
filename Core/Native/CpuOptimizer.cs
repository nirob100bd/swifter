using System.Diagnostics;
using System.Management;
using System.Runtime.Intrinsics.X86;

namespace Swifter.Core.Native;

public sealed class CpuOptimizer
{
    private static CpuOptimizer? _instance;
    private readonly int _physicalCores;
    private readonly int _logicalCores;
    private readonly bool _hasAvx2;
    private readonly bool _hasSse42;
    private readonly ulong _affinityMask;
    private readonly string _processorName;
    private readonly string _vendor;

    public static CpuOptimizer Instance => _instance ??= new CpuOptimizer();

    public int PhysicalCores => _physicalCores;
    public int LogicalCores => _logicalCores;
    public bool HasAvx2 => _hasAvx2;
    public bool HasSse42 => _hasSse42;
    public string ProcessorName => _processorName;
    public string Vendor => _vendor;

    private CpuOptimizer()
    {
        _hasAvx2 = Avx2.IsSupported;
        _hasSse42 = Sse42.IsSupported;
        _logicalCores = Environment.ProcessorCount;
        _physicalCores = DetectPhysicalCores();
        _affinityMask = (1UL << _logicalCores) - 1;
        _processorName = DetectProcessorName();
        _vendor = DetectVendor();
    }

    private static int DetectPhysicalCores()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT NumberOfCores FROM Win32_Processor");
            foreach (var obj in searcher.Get())
            {
                return Convert.ToInt32(obj["NumberOfCores"]);
            }
        }
        catch
        {
        }
        return Environment.ProcessorCount / 2;
    }

    private static string DetectProcessorName()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT Name FROM Win32_Processor");
            foreach (var obj in searcher.Get())
            {
                return obj["Name"]?.ToString() ?? "Unknown";
            }
        }
        catch
        {
        }
        return "Unknown";
    }

    private static string DetectVendor()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT Manufacturer FROM Win32_Processor");
            foreach (var obj in searcher.Get())
            {
                return obj["Manufacturer"]?.ToString() ?? "Unknown";
            }
        }
        catch
        {
        }
        return "Unknown";
    }

    public void OptimizeCurrentThread(int coreIndex = -1)
    {
        var thread = Win32Interop.GetCurrentThread();
        if (coreIndex >= 0 && coreIndex < _logicalCores)
        {
            Win32Interop.SetThreadAffinityMask(thread, (IntPtr)(1UL << coreIndex));
        }
        else
        {
            Win32Interop.SetThreadAffinityMask(thread, (IntPtr)_affinityMask);
        }
    }

    public void OptimizeNetworkThreads()
    {
        for (int i = 0; i < Math.Min(4, _logicalCores); i++)
        {
            var core = i;
            ThreadPool.QueueUserWorkItem(_ =>
            {
                var thread = Win32Interop.GetCurrentThread();
                Win32Interop.SetThreadAffinityMask(thread, (IntPtr)(1UL << core));
                Thread.CurrentThread.Priority = ThreadPriority.AboveNormal;
            });
        }
    }

    public void SetHighPriorityProcess()
    {
        try
        {
            Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.AboveNormal;
        }
        catch
        {
        }
    }

    public void ConfigureThreadPool()
    {
        ThreadPool.GetMinThreads(out int minWorker, out int minIO);
        ThreadPool.SetMinThreads(Math.Max(minWorker, _physicalCores * 2), Math.Max(minIO, _logicalCores));
        ThreadPool.GetMaxThreads(out int maxWorker, out int maxIO);
        ThreadPool.SetMaxThreads(Math.Max(maxWorker, _logicalCores * 4), Math.Max(maxIO, _logicalCores * 8));
    }

    public unsafe void PrefetchMemory(IntPtr address, int size)
    {
        if (_hasSse42 && size > 0)
        {
            var ptr = (byte*)address;
            int lines = size / 64;
            for (int i = 0; i < lines; i++)
            {
                Sse.Prefetch0(ptr + i * 64);
            }
        }
    }

    public int GetRecommendedDownloadThreadCount()
    {
        return Math.Clamp(_physicalCores * 4, 8, 32);
    }

    public int GetRecommendedTabProcessCount()
    {
        return Math.Clamp(_physicalCores - 1, 2, 8);
    }
}