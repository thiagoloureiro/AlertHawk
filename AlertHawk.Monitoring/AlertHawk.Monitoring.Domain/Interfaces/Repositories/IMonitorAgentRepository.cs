using AlertHawk.Monitoring.Domain.Entities;

namespace AlertHawk.Monitoring.Domain.Interfaces.Repositories;

public interface IMonitorAgentRepository
{
    Task ManageMonitorStatus(MonitorAgent monitorAgent);
    Task<List<MonitorAgent>> GetAllMonitorAgents();
    Task UpsertMonitorAgentTasks(List<MonitorAgentTasks> lstMonitorAgentTasks, int monitorRegion);
    Task<List<MonitorAgentTasks>> GetAllMonitorAgentTasks(int monitorRegion);
    Task<List<MonitorAgentTasks>> GetAllMonitorAgentTasksByAgentId(int id);
}