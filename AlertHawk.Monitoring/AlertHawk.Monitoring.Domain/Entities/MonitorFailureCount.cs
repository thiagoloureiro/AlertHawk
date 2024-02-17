namespace AlertHawk.Monitoring.Domain.Entities;

public class MonitorFailureCount
{
    public Monitor Monitor { get; set; }
    public int MonitorId { get; set; }
    public int FailureCount { get; set; }
}