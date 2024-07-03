using AlertHawk.Monitoring.Domain.Entities;
using Monitor = AlertHawk.Monitoring.Domain.Entities.Monitor;

namespace AlertHawk.Monitoring.Domain.Interfaces.Repositories;

public interface IMonitorGroupRepository
{
    Task<IEnumerable<MonitorGroup>> GetMonitorGroupList();
    Task<IEnumerable<MonitorGroup>> GetMonitorGroupListByEnvironment(MonitorEnvironment environment);
    Task<MonitorGroup> GetMonitorGroupById(int id);
    Task AddMonitorToGroup(MonitorGroupItems monitorGroupItems);
    Task RemoveMonitorFromGroup(MonitorGroupItems monitorGroupItems);
    Task<int> AddMonitorGroup(MonitorGroup monitorGroup);
    Task UpdateMonitorGroup(MonitorGroup monitorGroup);
    Task DeleteMonitorGroup(int id);
    Task<MonitorGroup?> GetMonitorGroupByName(string monitorGroupName);
    Task<IEnumerable<Monitor>?> GetMonitorListByGroupId(int monitorGroupId);
}