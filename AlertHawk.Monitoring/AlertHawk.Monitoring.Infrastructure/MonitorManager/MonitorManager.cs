using AlertHawk.Monitoring.Domain.Entities;
using AlertHawk.Monitoring.Domain.Interfaces.Repositories;

namespace AlertHawk.Monitoring.Infrastructure.MonitorManager;

public class MonitorManager: IMonitorManager
{
    private readonly IMonitorAgentRepository _monitorAgentRepository;

    public MonitorManager(IMonitorAgentRepository monitorAgentRepository)
    {
        _monitorAgentRepository = monitorAgentRepository;
    }

    public async Task Start()
    {
        var monitorAgent = new MonitorAgent
        {
            Hostname = Environment.MachineName,
            TimeStamp = DateTime.UtcNow
        };

        await _monitorAgentRepository.ManageMonitorStatus(monitorAgent);
    }
}