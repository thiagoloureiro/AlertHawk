namespace AlertHawk.Monitoring.Domain.Interfaces.Repositories;

public interface IMonitorManager
{
    Task StartMonitorHeartBeatManager();

    Task StartMasterMonitorAgentTaskManager();
}