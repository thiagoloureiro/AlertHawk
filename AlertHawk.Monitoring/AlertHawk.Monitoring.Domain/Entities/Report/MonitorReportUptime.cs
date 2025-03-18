using System.Diagnostics.CodeAnalysis;

namespace AlertHawk.Monitoring.Domain.Entities.Report;

[ExcludeFromCodeCoverage]
public class MonitorReportUptime
{
    public string? GroupName { get; set; }
    public string? MonitorName { get; set; }
    public int TotalOnlineMinutes { get; set; }
    public int TotalOfflineMinutes { get; set; }
    public double UptimePercentage { get; set; }
    public bool MonitorStatus { get; set; }
}