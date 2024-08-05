using System.Diagnostics.CodeAnalysis;

namespace AlertHawk.Monitoring.Domain.Entities;

[ExcludeFromCodeCoverage]
public class MonitorGroupItems
{
    public int MonitorId { get; set; }
    public int MonitorGroupId { get; set; }
}