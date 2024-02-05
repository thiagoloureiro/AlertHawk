using AlertHawk.Monitoring.Domain.Entities;

namespace AlertHawk.Monitoring.Domain.Interfaces.Services;

public interface IMonitorGroupService
{
    Task<IEnumerable<MonitorGroup>> GetMonitorGroupList();
    Task<MonitorGroup> GetMonitorGroupById(int id);
}