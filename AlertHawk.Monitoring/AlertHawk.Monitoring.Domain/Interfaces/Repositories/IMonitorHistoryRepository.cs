using AlertHawk.Monitoring.Domain.Entities;

namespace AlertHawk.Monitoring.Domain.Interfaces.Repositories;

public interface IMonitorHistoryRepository
{
    Task SaveMonitorHistory(MonitorHistory monitorHistory);

    Task<IEnumerable<MonitorHistory>> GetMonitorHistory(int id);

    Task<IEnumerable<MonitorHistory>> GetMonitorHistoryByIdAndDays(int id, int days);

    Task DeleteMonitorHistory(int days);

    Task<long> GetMonitorHistoryCount();

    Task<IEnumerable<MonitorHistory>> GetMonitorHistoryByIdAndMetrics(int id, string metric);

    Task<MonitorSettings?> GetMonitorHistoryRetention();

    Task SetMonitorHistoryRetention(int days);
}