using AlertHawk.Monitoring.Domain.Entities;
using Monitor = AlertHawk.Monitoring.Domain.Entities.Monitor;

namespace AlertHawk.Monitoring.Domain.Interfaces.Repositories;

public interface IMonitorRepository
{
    Task<IEnumerable<Monitor?>> GetMonitorList();
    Task<IEnumerable<Monitor?>> GetMonitorRunningList();
    Task<IEnumerable<MonitorHttp>> GetHttpMonitorByIds(List<int> ids);
    Task<IEnumerable<MonitorTcp>> GetTcpMonitorByIds(List<int> ids);
    Task<IEnumerable<Monitor>> GetMonitorListByIds(List<int> ids);
    Task<Monitor> GetMonitorById(int id);
    Task<MonitorHttp> GetHttpMonitorByMonitorId(int id);
    Task<MonitorTcp> GetTcpMonitorByMonitorId(int id);
    Task<IEnumerable<MonitorNotification>> GetMonitorNotifications(int id);
    Task UpdateMonitorStatus(int id, bool status, int daysToExpireCert);
    Task SaveMonitorHistory(MonitorHistory monitorHistory);
    Task<IEnumerable<MonitorHistory>> GetMonitorHistory(int id);
    Task<IEnumerable<MonitorHistory>> GetMonitorHistoryByIdAndDays(int id, int days);
    Task DeleteMonitorHistory(int days);
    Task PauseMonitor(int id, bool paused);
    Task<int> CreateMonitorHttp(MonitorHttp monitorHttp);
    Task SaveMonitorAlert(MonitorHistory monitorHistory);
    Task<IEnumerable<MonitorFailureCount>> GetMonitorFailureCount(int days);
    Task<IEnumerable<Monitor>?> GetMonitorListByMonitorGroupIds(List<int> groupMonitorIds,
        MonitorEnvironment environment);
    Task UpdateMonitorHttp(MonitorHttp monitorHttp);
    Task DeleteMonitor(int id);
    Task<int> CreateMonitorTcp(MonitorTcp monitorTcp);
    Task UpdateMonitorTcp(MonitorTcp monitorTcp);
    Task<IEnumerable<Monitor?>> GetMonitorList(MonitorEnvironment environment);
    Task AddMonitorNotification(MonitorNotification monitorNotification);
    Task RemoveMonitorNotification(MonitorNotification monitorNotification);
    Task<IEnumerable<MonitorHistory>> GetMonitorHistoryByIdAndHours(int id, int hours);
    Task<IEnumerable<Monitor>> GetMonitorListbyTag(string Tag);
    Task<IEnumerable<string?>> GetMonitorTagList();
}