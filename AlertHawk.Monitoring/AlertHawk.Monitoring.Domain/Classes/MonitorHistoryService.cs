using AlertHawk.Monitoring.Domain.Entities;
using AlertHawk.Monitoring.Domain.Interfaces.Repositories;
using AlertHawk.Monitoring.Domain.Interfaces.Services;
using EasyMemoryCache;

namespace AlertHawk.Monitoring.Domain.Classes;

public class MonitorHistoryService : IMonitorHistoryService
{
    private readonly string _monitorHistoryCount = "MonitorHistoryCountKey";
    private readonly ICaching _caching;
    private readonly IMonitorHistoryRepository _monitorHistoryRepository;

    public MonitorHistoryService(ICaching caching, IMonitorHistoryRepository monitorHistoryRepository)
    {
        _caching = caching;
        _monitorHistoryRepository = monitorHistoryRepository;
    }

    public async Task<IEnumerable<MonitorHistory>> GetMonitorHistory(int id)
    {
        return await _monitorHistoryRepository.GetMonitorHistory(id);
    }

    public async Task<IEnumerable<MonitorHistory>> GetMonitorHistory(int id, int days)
    {
        return await _monitorHistoryRepository.GetMonitorHistoryByIdAndDays(id, days);
    }

    public async Task DeleteMonitorHistory(int days)
    {
        await _monitorHistoryRepository.DeleteMonitorHistory(days);
    }

    public async Task<long> GetMonitorHistoryCount()
    {
        var count = await _caching.GetOrSetObjectFromCacheAsync(_monitorHistoryCount, 20,
            () => _monitorHistoryRepository.GetMonitorHistoryCount());
        return count;
    }

    public async Task SetMonitorHistoryRetention(int days)
    {
        await _monitorHistoryRepository.SetMonitorHistoryRetention(days);
    }

    public async Task<MonitorSettings> GetMonitorHistoryRetention()
    {
        return await _monitorHistoryRepository.GetMonitorHistoryRetention();
    }
}