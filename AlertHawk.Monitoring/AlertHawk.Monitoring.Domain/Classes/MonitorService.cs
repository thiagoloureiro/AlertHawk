using AlertHawk.Monitoring.Domain.Entities;
using AlertHawk.Monitoring.Domain.Interfaces.Repositories;
using AlertHawk.Monitoring.Domain.Interfaces.Services;

namespace AlertHawk.Monitoring.Domain.Classes;

public class MonitorService : IMonitorService
{
    private readonly IMonitorRepository _monitorRepository;

    public MonitorService(IMonitorRepository monitorRepository)
    {
        _monitorRepository = monitorRepository;
    }

    public async Task<IEnumerable<MonitorNotification>> GetMonitorNotifications(int id)
    {
        return await _monitorRepository.GetMonitorNotifications(id);
    }
}