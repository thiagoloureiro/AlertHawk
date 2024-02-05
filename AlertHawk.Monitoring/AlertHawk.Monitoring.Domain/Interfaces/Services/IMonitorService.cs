using AlertHawk.Monitoring.Domain.Entities;

namespace AlertHawk.Monitoring.Domain.Interfaces.Services;

public interface IMonitorService
{
    Task<IEnumerable<MonitorNotification>> GetMonitorNotifications(int id);
    Task<IEnumerable<MonitorHistory>> GetMonitorHistory(int id);
}