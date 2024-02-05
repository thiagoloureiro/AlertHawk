using AlertHawk.Monitoring.Domain.Entities;
using AlertHawk.Monitoring.Domain.Interfaces.Repositories;
using AlertHawk.Monitoring.Domain.Interfaces.Services;

namespace AlertHawk.Monitoring.Domain.Classes;

public class MonitorGroupService : IMonitorGroupService
{
    private readonly IMonitorGroupRepository _monitorGroupRepository;

    public MonitorGroupService(IMonitorGroupRepository monitorGroupRepository)
    {
        _monitorGroupRepository = monitorGroupRepository;
    }

    public async Task<IEnumerable<MonitorGroup>> GetMonitorGroupList()
    {
        return await _monitorGroupRepository.GetMonitorGroupList();
    }

    public async Task<MonitorGroup> GetMonitorGroupById(int id)
    {
        return await _monitorGroupRepository.GetMonitorGroupById(id);
    }
}