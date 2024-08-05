using AlertHawk.Monitoring.Domain.Entities;

namespace AlertHawk.Monitoring.Domain.Interfaces.Services;

public interface IMonitorHistoryService
{
    Task<IEnumerable<MonitorHistory>> GetMonitorHistory(int id);

    Task<IEnumerable<MonitorHistory>> GetMonitorHistory(int id, int days);

    Task DeleteMonitorHistory(int days);

    Task<long> GetMonitorHistoryCount();

    Task SetMonitorHistoryRetention(int days);

    Task<MonitorSettings?> GetMonitorHistoryRetention();
}