using System.Diagnostics.CodeAnalysis;

namespace AlertHawk.Monitoring.Domain.Entities;

[ExcludeFromCodeCoverage]
public class MonitorNotification
{
    public int MonitorId { get; set; }
    public int NotificationId { get; set; }
}