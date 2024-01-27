using AlertHawk.Monitoring.Domain.Entities;

namespace AlertHawk.Monitoring.Domain.Interfaces.Repositories;

public interface IMonitorTypeRepository
{
    public Task<IEnumerable<MonitorType>> GetMonitorType();
}