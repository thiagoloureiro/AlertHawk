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

    Task UpdateMonitorStatus(int id, bool status, int daysToExpireCert);

    Task PauseMonitor(int id, bool paused);

    Task<int> CreateMonitorHttp(MonitorHttp monitorHttp);

    Task<IEnumerable<MonitorFailureCount>> GetMonitorFailureCount(int days);

    Task<IEnumerable<Monitor>?> GetMonitorListByMonitorGroupIds(List<int> groupMonitorIds,
        MonitorEnvironment environment);

    Task UpdateMonitorHttp(MonitorHttp monitorHttp);

    Task DeleteMonitor(int id);

    Task<int> CreateMonitorTcp(MonitorTcp monitorTcp);

    Task UpdateMonitorTcp(MonitorTcp monitorTcp);

    Task<IEnumerable<Monitor?>> GetMonitorList(MonitorEnvironment environment);

    Task<IEnumerable<Monitor>> GetMonitorListbyTag(string Tag);

    Task<IEnumerable<string?>> GetMonitorTagList();

    Task<IEnumerable<MonitorHttp>> GetMonitorHttpList();

    Task<IEnumerable<MonitorTcp>> GetMonitorTcpList();

    Task<int> CreateMonitor(Monitor monitor);

    Task WipeMonitorData();
    Task<IEnumerable<MonitorK8s>> GetK8sMonitorByIds(List<int> ids);
}