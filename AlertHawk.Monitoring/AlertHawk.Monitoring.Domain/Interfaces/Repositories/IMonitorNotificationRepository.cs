using AlertHawk.Monitoring.Domain.Entities;

namespace AlertHawk.Monitoring.Domain.Interfaces.Repositories;

public interface IMonitorNotificationRepository
{
    Task AddMonitorNotification(MonitorNotification monitorNotification);

    Task RemoveMonitorNotification(MonitorNotification monitorNotification);

    Task<IEnumerable<MonitorNotification>> GetMonitorNotifications(int id);
}