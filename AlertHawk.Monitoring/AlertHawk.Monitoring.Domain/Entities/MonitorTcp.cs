using System.Diagnostics.CodeAnalysis;

namespace AlertHawk.Monitoring.Domain.Entities;

[ExcludeFromCodeCoverage]
public class MonitorTcp : Monitor
{
    public int MonitorId { get; set; }
    public int Port { get; set; }
    public string IP { get; set; }
    public int Timeout { get; set; }
    public bool LastStatus { get; set; }
    public string? Response { get; set; }
}