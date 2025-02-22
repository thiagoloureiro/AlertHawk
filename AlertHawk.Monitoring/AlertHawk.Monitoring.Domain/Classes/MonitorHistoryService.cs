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

    public async Task<IEnumerable<MonitorHistory>> GetMonitorHistory(int id, int days, bool downSampling,
        int downSamplingFactor)
    {
        IEnumerable<MonitorHistory> monitorData;
        if (days == 0)
        {
            monitorData = await _monitorHistoryRepository.GetMonitorHistoryByIdAndHours(id, days);
        }
        else
        {
            monitorData = await _monitorHistoryRepository.GetMonitorHistoryByIdAndDays(id, days);
        }

        if (downSampling)
        {
            var data = monitorData.ToArray();
            var downSampledData = new List<MonitorHistory>();

            // First, add all records where Status is false
            var failureRecords = data.Where(x => !x.Status).ToList();
            downSampledData.AddRange(failureRecords);

            // Then perform downsampling only on successful records
            var successRecords = data.Where(x => x.Status).ToArray();
            var downSampledDataCount = successRecords.Length / downSamplingFactor;

            for (var i = 0; i < downSampledDataCount; i++)
            {
                var downSampledItem = successRecords[i * downSamplingFactor];
                downSampledData.Add(downSampledItem);
            }

            // Sort the combined results by timestamp to maintain chronological order
            monitorData = downSampledData.OrderBy(x => x.TimeStamp);
        }

        var monitorHistories = monitorData as MonitorHistory[] ?? monitorData.ToArray();
        Parallel.ForEach(monitorHistories, item =>
        {
            if (!item.Status)
            {
                item.ResponseTime = 0;
            }
        });

        return monitorHistories;
    }

    public async Task DeleteMonitorHistory(int days)
    {
        await _monitorHistoryRepository.DeleteMonitorHistory(days);
        await _caching.InvalidateAllAsync();
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

    public async Task<MonitorSettings?> GetMonitorHistoryRetention()
    {
        return await _monitorHistoryRepository.GetMonitorHistoryRetention();
    }
}