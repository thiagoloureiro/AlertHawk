using AlertHawk.Monitoring.Domain.Entities;
using Monitor = AlertHawk.Monitoring.Domain.Entities.Monitor;

namespace AlertHawk.Monitoring.Domain.Interfaces.Services;

public interface IMonitorService
{
    Task<IEnumerable<Monitor?>> GetMonitorList();
    Task PauseMonitor(int id, bool paused);
    Task<MonitorDashboard?> GetMonitorDashboardData(int id);
    IEnumerable<MonitorDashboard> GetMonitorDashboardDataList(List<int> ids);
    Task SetMonitorDashboardDataCacheList();
    Task<MonitorStatusDashboard?> GetMonitorStatusDashboard(string jwtToken, MonitorEnvironment environment);
    Task<int> CreateMonitorHttp(MonitorHttp monitorHttp);
    Task<IEnumerable<MonitorFailureCount>> GetMonitorFailureCount(int days);
    Task<IEnumerable<Monitor>?> GetMonitorListByMonitorGroupIds(string token, MonitorEnvironment environment);
    Task UpdateMonitorHttp(MonitorHttp monitorHttp);
    Task DeleteMonitor(int id, string? jwtToken);
    Task<int> CreateMonitorTcp(MonitorTcp monitorTcp);
    Task UpdateMonitorTcp(MonitorTcp monitorTcp);
    Task<MonitorHttp> GetHttpMonitorByMonitorId(int id);
    Task<MonitorTcp> GetTcpMonitorByMonitorId(int id);
    Task PauseMonitorByGroupId(int groupId, bool paused);

    Task<IEnumerable<Monitor?>> GetMonitorListByTag(string tag);
    Task<IEnumerable<string?>> GetMonitorTagList();
    Task<string> GetMonitorBackupJson();
    Task UploadMonitorJsonBackup(MonitorBackup monitorBackups);
}