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

    public async Task AddMonitorToGroup(MonitorGroupItems monitorGroupItems)
    {
        await _monitorGroupRepository.AddMonitorToGroup(monitorGroupItems);
    }

    public async Task RemoveMonitorFromGroup(MonitorGroupItems monitorGroupItems)
    {
        await _monitorGroupRepository.RemoveMonitorFromGroup(monitorGroupItems);
    }

    public async Task AddMonitorGroup(MonitorGroup monitorGroup)
    {
        await _monitorGroupRepository.AddMonitorGroup(monitorGroup);
    }

    public async Task UpdateMonitorGroup(MonitorGroup monitorGroup)
    {
        await _monitorGroupRepository.UpdateMonitorGroup(monitorGroup);
    }

    public async Task DeleteMonitorGroup(int id)
    {
        await _monitorGroupRepository.DeleteMonitorGroup(id);
    }
}