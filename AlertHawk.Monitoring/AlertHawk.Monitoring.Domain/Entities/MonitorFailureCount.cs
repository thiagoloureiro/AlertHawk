using System.Diagnostics.CodeAnalysis;

namespace AlertHawk.Monitoring.Domain.Entities;

[ExcludeFromCodeCoverage]
public class MonitorFailureCount
{
    public Monitor? Monitor { get; set; }
    public int MonitorId { get; set; }
    public int FailureCount { get; set; }
}