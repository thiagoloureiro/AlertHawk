using AlertHawk.Monitoring.Domain.Entities;

namespace AlertHawk.Monitoring.Domain.Interfaces.Services;

public interface IMonitorTypeService
{
    public Task<IEnumerable<MonitorType>> GetMonitorType();
}