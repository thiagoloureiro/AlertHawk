using AlertHawk.Monitoring.Domain.Entities;

namespace AlertHawk.Monitoring.Domain.Interfaces.Services;

public interface IMonitorHistoryService
{
    Task<IEnumerable<MonitorHistory>> GetMonitorHistory(int id);

    Task<IEnumerable<MonitorHistory>> GetMonitorHistory(int id, int days, bool downSampling, int downSamplingFactor);

    Task DeleteMonitorHistory(int days);

    Task<long> GetMonitorHistoryCount();

    Task SetMonitorHistoryRetention(int days);

    Task<MonitorSettings?> GetMonitorHistoryRetention();
    Task<MonitorHttpHeaders> GetMonitorSecurityHeaders(int id);
}