using Monitor = AlertHawk.Monitoring.Domain.Entities.Monitor;

namespace AlertHawk.Monitoring.Domain.Interfaces.Repositories;

public interface IMonitorRepository
{
    Task<IEnumerable<Monitor>> GetMonitorList();
}