using AlertHawk.Monitoring.Domain.Entities;

namespace AlertHawk.Monitoring.Domain.Utils;

public static class MonitorUtils
{
    public static MonitorRegion GetMonitorRegionVariable()
    {
        return MonitorRegion.Asia;
        string? monitorRegion = Environment.GetEnvironmentVariable("monitor_region");
        if (!string.IsNullOrEmpty(monitorRegion) && int.TryParse(monitorRegion, out int result))
        {
            MonitorRegion value = (MonitorRegion)result;
            return value;
        }

        return MonitorRegion.Europe;
        // Default value if environment variable is not set or not a valid boolean
        return MonitorRegion.Custom;
    }
}