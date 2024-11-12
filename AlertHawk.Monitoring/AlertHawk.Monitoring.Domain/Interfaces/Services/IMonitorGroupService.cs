using AlertHawk.Monitoring.Domain.Entities;
using Monitor = AlertHawk.Monitoring.Domain.Entities.Monitor;

namespace AlertHawk.Monitoring.Domain.Interfaces.Services;

public interface IMonitorGroupService
{
    Task<IEnumerable<MonitorGroup>> GetMonitorGroupListByEnvironment(string jwtToken, MonitorEnvironment environment, string metric);

    Task<IEnumerable<MonitorGroup>?> GetMonitorGroupList(string jwtToken);

    Task<MonitorGroup> GetMonitorGroupById(int id);

    Task AddMonitorToGroup(MonitorGroupItems monitorGroupItems);

    Task RemoveMonitorFromGroup(MonitorGroupItems monitorGroupItems);

    Task<int> AddMonitorGroup(MonitorGroup monitorGroup, string? jwtToken);

    Task UpdateMonitorGroup(MonitorGroup monitorGroup);

    Task DeleteMonitorGroup(string jwtToken, int id);

    Task<List<int>?> GetUserGroupMonitorListIds(string token);

    Task<IEnumerable<MonitorGroup>?> GetMonitorGroupList();

    Task<IEnumerable<MonitorGroup>?> GetMonitorDashboardGroupListByUser(string jwtToken);

    Task<MonitorGroup?> GetMonitorGroupByName(string monitorGroupName);

    Task<IEnumerable<Monitor>?> GetMonitorListByGroupId(int monitorGroupId);

    Task<int> GetMonitorGroupIdByMonitorId(int id);
}