using AlertHawk.Monitoring.Domain.Entities;
using Monitor = AlertHawk.Monitoring.Domain.Entities.Monitor;

namespace AlertHawk.Monitoring.Domain.Interfaces.Services;

public interface IMonitorService
{
    Task<IEnumerable<MonitorNotification>> GetMonitorNotifications(int id);
    Task<IEnumerable<MonitorHistory>> GetMonitorHistory(int id);
    Task<IEnumerable<MonitorHistory>> GetMonitorHistory(int id, int days);
    Task<IEnumerable<Monitor>> GetMonitorList();
    Task DeleteMonitorHistory(int days);
    Task PauseMonitor(int id, bool paused);
    Task<MonitorDashboard> GetMonitorDashboardData(int id);
    IEnumerable<MonitorDashboard> GetMonitorDashboardDataList(List<int> ids);
    Task SetMonitorDashboardDataCacheList();
}