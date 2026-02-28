using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Serilog;

namespace AlertHawk.Metrics.Collectors;

/// <summary>
/// Collects CPU, RAM, and disk metrics from the current host (Windows or Linux).
/// Used when AGENT_TYPE=vm.
/// </summary>
public static class HostMetricsCollector
{
    public static async Task<(double CpuPercent, ulong MemoryTotalBytes, ulong MemoryUsedBytes, List<(string DriveName, ulong TotalBytes, ulong FreeBytes)> Disks)> CollectAsync()
    {
        var cpuPercent = await GetCpuUsagePercentAsync();
        var (totalMem, usedMem) = GetMemoryBytes();
        var disks = GetDiskMetrics();
        return (cpuPercent, totalMem, usedMem, disks);
    }

    private static async Task<double> GetCpuUsagePercentAsync()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return await GetCpuUsageWindowsAsync();
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            return await GetCpuUsageLinuxAsync();
        }

        Log.Warning("Unsupported OS for CPU metrics; returning 0");
        return 0;
    }

    [SupportedOSPlatform("windows")]
    private static async Task<double> GetCpuUsageWindowsAsync()
    {
        try
        {
            using var cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
            cpuCounter.NextValue(); // first call returns 0
            await Task.Delay(500);
            var value = cpuCounter.NextValue();
            return Math.Clamp(value, 0, 100);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Could not read Windows CPU counter");
            return 0;
        }
    }

    private static async Task<double> GetCpuUsageLinuxAsync()
    {
        try
        {
            var (user1, nice1, system1, idle1) = ParseProcStat();
            await Task.Delay(1000);
            var (user2, nice2, system2, idle2) = ParseProcStat();

            var total1 = user1 + nice1 + system1 + idle1;
            var total2 = user2 + nice2 + system2 + idle2;
            var idleDelta = idle2 - idle1;
            var totalDelta = total2 - total1;

            if (totalDelta <= 0) return 0;
            var used = totalDelta - idleDelta;
            return Math.Clamp(100.0 * used / totalDelta, 0, 100);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Could not read Linux /proc/stat");
            return 0;
        }
    }

    private static (ulong user, ulong nice, ulong system, ulong idle) ParseProcStat()
    {
        var line = File.ReadAllText("/proc/stat");
        var parts = line.Split('\n')[0].Split(' ', StringSplitOptions.RemoveEmptyEntries);
        // cpu  user nice system idle iowait irq softirq ...
        if (parts.Length < 5 || parts[0] != "cpu") return (0, 0, 0, 0);
        return (
            ulong.TryParse(parts[1], out var u) ? u : 0,
            ulong.TryParse(parts[2], out var n) ? n : 0,
            ulong.TryParse(parts[3], out var s) ? s : 0,
            ulong.TryParse(parts[4], out var i) ? i : 0
        );
    }

    private static (ulong TotalBytes, ulong UsedBytes) GetMemoryBytes()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return GetMemoryWindows();
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            return GetMemoryLinux();
        }

        return (0, 0);
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MemoryStatusEx
    {
        public uint Length;
        public uint MemoryLoad;
        public ulong TotalPhys;
        public ulong AvailPhys;
        public ulong TotalPageFile;
        public ulong AvailPageFile;
        public ulong TotalVirtual;
        public ulong AvailVirtual;
        public ulong AvailExtendedVirtual;
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GlobalMemoryStatusEx(ref MemoryStatusEx lpBuffer);

    private static (ulong TotalBytes, ulong UsedBytes) GetMemoryWindows()
    {
        try
        {
            var status = new MemoryStatusEx { Length = (uint)Marshal.SizeOf<MemoryStatusEx>() };
            if (!GlobalMemoryStatusEx(ref status))
                return (0, 0);
            var used = status.TotalPhys > status.AvailPhys ? status.TotalPhys - status.AvailPhys : 0;
            return (status.TotalPhys, used);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Could not read Windows memory");
            return (0, 0);
        }
    }

    private static (ulong TotalBytes, ulong UsedBytes) GetMemoryLinux()
    {
        try
        {
            ulong total = 0, available = 0;
            foreach (var line in File.ReadAllLines("/proc/meminfo"))
            {
                if (line.StartsWith("MemTotal:", StringComparison.Ordinal))
                    total = ParseMemValue(line);
                else if (line.StartsWith("MemAvailable:", StringComparison.Ordinal))
                    available = ParseMemValue(line);
                else if (line.StartsWith("MemFree:", StringComparison.Ordinal) && available == 0)
                    available = ParseMemValue(line);
            }

            if (total == 0) return (0, 0);
            var used = total > available ? total - available : 0;
            return (total, used);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Could not read /proc/meminfo");
            return (0, 0);
        }
    }

    private static ulong ParseMemValue(string line)
    {
        var parts = line.Split(':', StringSplitOptions.TrimEntries);
        if (parts.Length != 2) return 0;
        var valuePart = parts[1].Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (valuePart.Length == 0 || !ulong.TryParse(valuePart[0], out var kb)) return 0;
        return kb * 1024;
    }

    private static List<(string DriveName, ulong TotalBytes, ulong FreeBytes)> GetDiskMetrics()
    {
        var list = new List<(string, ulong, ulong)>();
        try
        {
            foreach (var drive in DriveInfo.GetDrives().Where(d => d.IsReady))
            {
                try
                {
                    var total = (ulong)drive.TotalSize;
                    var free = (ulong)drive.AvailableFreeSpace;
                    var name = drive.Name.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                    if (string.IsNullOrEmpty(name)) name = drive.Name;
                    list.Add((name, total, free));
                }
                catch
                {
                    // skip drive if we can't read it
                }
            }
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Could not enumerate drives");
        }

        return list;
    }
}
