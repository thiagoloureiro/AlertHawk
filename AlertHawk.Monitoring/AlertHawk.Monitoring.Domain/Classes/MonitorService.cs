using AlertHawk.Monitoring.Domain.Entities;
using AlertHawk.Monitoring.Domain.Interfaces.Repositories;
using AlertHawk.Monitoring.Domain.Interfaces.Services;
using Monitor = AlertHawk.Monitoring.Domain.Entities.Monitor;

namespace AlertHawk.Monitoring.Domain.Classes;

public class MonitorService : IMonitorService
{
    private readonly IMonitorRepository _monitorRepository;

    public MonitorService(IMonitorRepository monitorRepository)
    {
        _monitorRepository = monitorRepository;
    }

    public async Task<IEnumerable<MonitorNotification>> GetMonitorNotifications(int id)
    {
        return await _monitorRepository.GetMonitorNotifications(id);
    }

    public async Task<IEnumerable<MonitorHistory>> GetMonitorHistory(int id)
    {
        return await _monitorRepository.GetMonitorHistory(id);
    }

    public async Task<IEnumerable<Monitor>> GetMonitorList()
    {
        return await _monitorRepository.GetMonitorList();
    }

    public async Task DeleteMonitorHistory(int days)
    {
        await _monitorRepository.DeleteMonitorHistory(days);
    }

    public async Task PauseMonitor(int id, bool paused)
    {
        await _monitorRepository.PauseMonitor(id, paused);
    }
}