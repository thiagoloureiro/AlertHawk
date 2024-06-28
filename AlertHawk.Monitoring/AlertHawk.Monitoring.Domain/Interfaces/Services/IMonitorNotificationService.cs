using AlertHawk.Monitoring.Domain.Entities;

namespace AlertHawk.Monitoring.Domain.Interfaces.Services;

public interface IMonitorNotificationService
{
    Task<IEnumerable<MonitorNotification>> GetMonitorNotifications(int id);
    Task AddMonitorNotification(MonitorNotification monitorNotification);
    Task RemoveMonitorNotification(MonitorNotification monitorNotification);
    Task AddMonitorGroupNotification(MonitorGroupNotification monitorGroupNotification);
    Task RemoveMonitorGroupNotification(MonitorGroupNotification monitorGroupNotification);
}