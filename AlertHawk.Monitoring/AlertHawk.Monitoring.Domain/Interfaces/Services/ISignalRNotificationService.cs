using AlertHawk.Monitoring.Domain.Entities;

namespace AlertHawk.Monitoring.Domain.Interfaces.Services;

public interface ISignalRNotificationService
{
    Task SendNotificationToMonitorGroupAsync(int monitorId, NotificationAlert notification);
    Task SendNotificationToEnvironmentGroupAsync(int environment, NotificationAlert notification);
    Task SendNotificationToRegionGroupAsync(int region, NotificationAlert notification);
    Task SendNotificationToUserAsync(string connectionId, NotificationAlert notification);
}

public class NotificationAlert
{
    public int NotificationId { get; set; }
    public int MonitorId { get; set; }
    public string Service { get; set; } = string.Empty;
    public int Region { get; set; }
    public int Environment { get; set; }
    public string? URL { get; set; }
    public string? IP { get; set; }
    public int? Port { get; set; }
    public string? ClusterName { get; set; }
    public bool Success { get; set; }
    public DateTime TimeStamp { get; set; }
    public string Message { get; set; } = string.Empty;
    public int? StatusCode { get; set; }
    public string? ReasonPhrase { get; set; }
}
