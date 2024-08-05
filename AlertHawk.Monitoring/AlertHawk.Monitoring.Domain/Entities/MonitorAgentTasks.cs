using System.Diagnostics.CodeAnalysis;

namespace AlertHawk.Monitoring.Domain.Entities;

[ExcludeFromCodeCoverage]
public class MonitorAgentTasks
{
    public int MonitorId { get; set; }
    public int MonitorAgentId { get; set; }
}