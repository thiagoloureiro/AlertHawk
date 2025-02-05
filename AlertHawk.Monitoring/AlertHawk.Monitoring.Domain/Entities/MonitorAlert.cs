using System.Diagnostics.CodeAnalysis;

namespace AlertHawk.Monitoring.Domain.Entities;

[ExcludeFromCodeCoverage]
public class MonitorAlert
{
    public int Id { get; set; }
    public int MonitorId { get; set; }
    public DateTime TimeStamp { get; set; }
    public bool Status { get; set; }
    public string? Message { get; set; }
    public string? MonitorName { get; set; }
    public MonitorEnvironment Environment { get; set; }
    public string? UrlToCheck { get; set; }
    public int PeriodOffline { get; set; }
}