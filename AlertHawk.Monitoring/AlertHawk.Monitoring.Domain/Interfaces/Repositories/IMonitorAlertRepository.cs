using AlertHawk.Monitoring.Domain.Entities;

namespace AlertHawk.Monitoring.Domain.Interfaces.Repositories;

public interface IMonitorAlertRepository
{
    Task<IEnumerable<MonitorAlert>> GetMonitorAlerts(int? monitorId, int? days, MonitorEnvironment? environment,
        List<int>? groupIds);

    Task<MemoryStream> CreateExcelFileAsync(IEnumerable<MonitorAlert> alerts);

    Task SaveMonitorAlert(MonitorHistory monitorHistory, MonitorEnvironment environment);
    Task<IEnumerable<MonitorAlert>> GetMonitorAlertsByMonitorGroup(List<int> monitorIds, int? days, MonitorEnvironment? environment);
}