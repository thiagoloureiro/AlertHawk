using AlertHawk.Monitoring.Domain.Entities;
using AlertHawk.Monitoring.Domain.Interfaces.Repositories;
using AlertHawk.Monitoring.Domain.Interfaces.Services;

namespace AlertHawk.Monitoring.Domain.Classes;

public class MonitorAlertService : IMonitorAlertService
{
    private readonly IMonitorAlertRepository _monitorAlertRepository;

    public MonitorAlertService(IMonitorAlertRepository monitorAlertRepository)
    {
        _monitorAlertRepository = monitorAlertRepository;
    }

    public async Task<IEnumerable<MonitorAlert>> GetMonitorAlerts(int? monitorId, int? days)
    {
        return await _monitorAlertRepository.GetMonitorAlerts(monitorId, days);
    }
}