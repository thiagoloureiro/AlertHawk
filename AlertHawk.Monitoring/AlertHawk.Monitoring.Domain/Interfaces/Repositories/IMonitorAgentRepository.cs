using AlertHawk.Monitoring.Domain.Entities;

namespace AlertHawk.Monitoring.Domain.Interfaces.Repositories;

public interface IMonitorAgentRepository
{
    Task ManageMonitorStatus(MonitorAgent monitorAgent);
    Task<List<MonitorAgent>> GetAllMonitorAgents();
}