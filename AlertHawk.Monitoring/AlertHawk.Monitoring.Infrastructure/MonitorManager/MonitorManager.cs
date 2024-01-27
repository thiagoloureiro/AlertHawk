using AlertHawk.Monitoring.Domain.Entities;
using AlertHawk.Monitoring.Domain.Interfaces.Repositories;

namespace AlertHawk.Monitoring.Infrastructure.MonitorManager;

public class MonitorManager : IMonitorManager
{
    private readonly IMonitorAgentRepository _monitorAgentRepository;
    private IMonitorRepository _monitorRepository;

    public MonitorManager(IMonitorAgentRepository monitorAgentRepository, IMonitorRepository monitorRepository)
    {
        _monitorAgentRepository = monitorAgentRepository;
        _monitorRepository = monitorRepository;
    }

    public async Task StartMonitorHeartBeatManager()
    {
        var monitorAgent = new MonitorAgent
        {
            Hostname = Environment.MachineName,
            TimeStamp = DateTime.UtcNow
        };

        await _monitorAgentRepository.ManageMonitorStatus(monitorAgent);
    }

    public async Task StartMonitorAgentTaskManager()
    {
        Console.WriteLine($"Started StartMonitorAgentTaskManager, current Master: {GlobalVariables.MasterNode}");
        // Only MasterNode is responsible for Managing Tasks
        if (GlobalVariables.MasterNode)
        {
            var lstMonitorAgentTasks = new List<MonitorAgentTasks>();
            var iEnumerable = await _monitorRepository.GetMonitorList();
            var monitorList = iEnumerable.ToList();
            var monitorAgents = await _monitorAgentRepository.GetAllMonitorAgents();

            var countMonitor = monitorList.Count();
            var countAgents = monitorAgents.Count();

            int tasksPerMonitor = countMonitor / countAgents;
            int extraTasks = countMonitor % countAgents;

            int currentIndex = 0;
            int indexAgent = 0;

            for (int i = 0; i < countMonitor; i++)
            {
                int tasksToTake = tasksPerMonitor + (i < extraTasks ? 1 : 0);

                var tasksForMonitor = monitorList.Skip(currentIndex).Take(tasksToTake).ToList();

                if (!tasksForMonitor.Any())
                {
                    break;
                }

                var agent = monitorAgents.ElementAt(indexAgent);

                foreach (var task in tasksForMonitor)
                {
                    lstMonitorAgentTasks.Add(new MonitorAgentTasks
                    {
                        MonitorId = task.Id,
                        MonitorAgentId = agent.Id
                    });
                }

                indexAgent += 1;
                currentIndex += tasksToTake;
            }

            foreach (var item in lstMonitorAgentTasks)
            {
                Console.WriteLine($"MonitorId: {item.MonitorId}, AgentId: {item.MonitorAgentId}");
            }
        }
    }
}