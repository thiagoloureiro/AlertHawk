namespace AlertHawk.Monitoring.Domain.Entities.Report;

public class MonitorReportUptime
{
    public string GroupName { get; set; }
    public string MonitorName { get; set; }
    public int TotalOnlineMinutes { get; set; }
    public int TotalOfflineMinutes { get; set; }
    public double UptimePercentage { get; set; }
}