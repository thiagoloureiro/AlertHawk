using System.Diagnostics.CodeAnalysis;

namespace AlertHawk.Metrics.API.Entities;

[ExcludeFromCodeCoverage]
public class MetricsAlert
{
    public int Id { get; set; }
    public string NodeName { get; set; } = string.Empty;
    public string ClusterName { get; set; } = string.Empty;
    public DateTime TimeStamp { get; set; }
    public bool Status { get; set; }
    public string? Message { get; set; }
}
