using System.Diagnostics.CodeAnalysis;

namespace AlertHawk.Monitoring.Domain.Entities;
[ExcludeFromCodeCoverage]
public class MonitorGroupNotification
{
    public int MonitorGroupId { get; set; }
    public int NotificationId { get; set; }
}