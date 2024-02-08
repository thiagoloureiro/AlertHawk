using AlertHawk.Monitoring.Domain.Entities;

namespace AlertHawk.Monitoring.Domain.Interfaces.Services;

public interface IMonitorAgentService
{
    Task<IEnumerable<MonitorAgent>> GetAllMonitorAgents();
}