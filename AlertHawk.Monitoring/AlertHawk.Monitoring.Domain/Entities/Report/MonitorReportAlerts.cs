using System.Diagnostics.CodeAnalysis;

namespace AlertHawk.Monitoring.Domain.Entities.Report;

[ExcludeFromCodeCoverage]
public class MonitorReportAlerts
{
    public string? MonitorName { get; set; }
    public int NumAlerts { get; set; }
}