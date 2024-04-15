using AlertHawk.Monitoring.Domain.Entities;
using AlertHawk.Monitoring.Domain.Interfaces.Repositories;
using AlertHawk.Monitoring.Domain.Interfaces.Services;

namespace AlertHawk.Monitoring.Domain.Classes;

public class MonitorAgentService : IMonitorAgentService
{
    private readonly IMonitorAgentRepository _monitorAgentRepository;

    public MonitorAgentService(IMonitorAgentRepository monitorAgentRepository)
    {
        _monitorAgentRepository = monitorAgentRepository;
    }

    public async Task<IEnumerable<MonitorAgent>> GetAllMonitorAgents()
    {
        var agents = await _monitorAgentRepository.GetAllMonitorAgents();
        if (agents.Any())
        {
            var agentTasks =
                await _monitorAgentRepository.GetAllMonitorAgentTasks(agents.Select(x => (int)x.MonitorRegion!)
                    .ToList());

            foreach (var agent in agents)
            {
                agent.ListTasks = agentTasks.Count(x => x.MonitorAgentId == agent.Id);
            }

            return agents;
        }

        return new List<MonitorAgent>();
    }
}