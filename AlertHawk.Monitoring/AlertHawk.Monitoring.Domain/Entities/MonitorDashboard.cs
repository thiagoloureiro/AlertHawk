using System.Diagnostics.CodeAnalysis;

namespace AlertHawk.Monitoring.Domain.Entities;

[ExcludeFromCodeCoverage]
public class MonitorDashboard
{
    public int MonitorId { get; set; }
    public double Uptime1Hr { get; set; }
    public double Uptime24Hrs { get; set; }
    public double Uptime7Days { get; set; }
    public double Uptime30Days { get; set; }
    public double Uptime3Months { get; set; }
    public double Uptime6Months { get; set; }
    public double CertExpDays { get; set; }
    public double ResponseTime { get; set; }
    public IEnumerable<MonitorHistory>? HistoryData { get; set; }
}