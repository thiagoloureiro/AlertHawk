using AlertHawk.Monitoring.Domain.Entities;

namespace AlertHawk.Monitoring.Domain.Interfaces.Repositories;

public interface IMonitorAlertRepository
{
    Task<IEnumerable<MonitorAlert>> GetMonitorAlerts(int? monitorId, int? days, Task<List<int>?> groupIds);
}