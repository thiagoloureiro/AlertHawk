using AlertHawk.Monitoring.Domain.Entities;
using AlertHawk.Monitoring.Domain.Interfaces.Repositories;
using AlertHawk.Monitoring.Domain.Interfaces.Services;

namespace AlertHawk.Monitoring.Domain.Classes;

public class MonitorAlertService : IMonitorAlertService
{
    private readonly IMonitorAlertRepository _monitorAlertRepository;
    private readonly IMonitorGroupService _monitorGroupService;

    public MonitorAlertService(IMonitorAlertRepository monitorAlertRepository, IMonitorGroupService monitorGroupService)
    {
        _monitorAlertRepository = monitorAlertRepository;
        _monitorGroupService = monitorGroupService;
    }

    public async Task<IEnumerable<MonitorAlert>> GetMonitorAlerts(int? monitorId, int? days, string jwtToken)
    {
        var groupIds = await _monitorGroupService.GetUserGroupMonitorListIds(jwtToken);
        if (groupIds != null && groupIds.Any())
        {
            return await _monitorAlertRepository.GetMonitorAlerts(monitorId, days, groupIds);
        }

        return new List<MonitorAlert>();
    }
}