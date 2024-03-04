using AlertHawk.Monitoring.Domain.Entities;

namespace AlertHawk.Monitoring.Domain.Interfaces.Services;

public interface IMonitorAlertService
{
    Task<IEnumerable<MonitorAlert>> GetMonitorAlerts(int? monitorId, int? days, string jwtToken);
}