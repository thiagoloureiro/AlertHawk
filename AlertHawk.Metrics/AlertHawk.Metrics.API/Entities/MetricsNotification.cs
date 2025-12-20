using System.Diagnostics.CodeAnalysis;

namespace AlertHawk.Metrics.API.Entities;

[ExcludeFromCodeCoverage]
public class MetricsNotification
{
    public string ClusterName { get; set; } = string.Empty;
    public int NotificationId { get; set; }
}
