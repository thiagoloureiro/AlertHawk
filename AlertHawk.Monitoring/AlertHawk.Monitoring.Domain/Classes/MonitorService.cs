using System.Diagnostics;
using AlertHawk.Monitoring.Domain.Entities;
using AlertHawk.Monitoring.Domain.Interfaces.Repositories;
using AlertHawk.Monitoring.Domain.Interfaces.Services;
using EasyMemoryCache;
using Monitor = AlertHawk.Monitoring.Domain.Entities.Monitor;

namespace AlertHawk.Monitoring.Domain.Classes;

public class MonitorService : IMonitorService
{
    private readonly IMonitorRepository _monitorRepository;
    private readonly ICaching _caching;
    private string _cacheKeyDashboardList = "MonitorDashboardList";

    public MonitorService(IMonitorRepository monitorRepository, ICaching caching)
    {
        _monitorRepository = monitorRepository;
        _caching = caching;
    }

    public async Task<IEnumerable<MonitorNotification>> GetMonitorNotifications(int id)
    {
        return await _monitorRepository.GetMonitorNotifications(id);
    }

    public async Task<IEnumerable<MonitorHistory>> GetMonitorHistory(int id)
    {
        return await _monitorRepository.GetMonitorHistory(id);
    }

    public async Task<IEnumerable<MonitorHistory>> GetMonitorHistory(int id, int days)
    {
        return await _monitorRepository.GetMonitorHistory(id, days);
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
        _caching.Invalidate($"Monitor_{id}");
    }

    public async Task<MonitorDashboard> GetMonitorDashboardData(int id)
    {
        var result = await _caching.GetOrSetObjectFromCacheAsync($"GroupHistory_{id}_90", 20,
            () => _monitorRepository.GetMonitorHistory(id, 90));

        var monitor =
            await _caching.GetOrSetObjectFromCacheAsync($"Monitor_{id}", 20,
                () => _monitorRepository.GetMonitorById(id));

        if (monitor == null) return null;

        var lst24Hrs = result.Where(x => x.TimeStamp > DateTime.Now.AddDays(-1)).ToList();
        var lst7Days = result.Where(x => x.TimeStamp > DateTime.Now.AddDays(-7)).ToList();
        var lst30Days = result.Where(x => x.TimeStamp > DateTime.Now.AddDays(-30)).ToList();
        var lst3Months = result.Where(x => x.TimeStamp > DateTime.Now.AddDays(-90)).ToList();
        var lst6Months = result.Where(x => x.TimeStamp > DateTime.Now.AddDays(-180)).ToList();

        double uptime24Hrs = (double)lst24Hrs.Count(item => item.Status) / lst24Hrs.Count * 100;
        double upTime7Days = (double)lst7Days.Count(item => item.Status) / lst7Days.Count * 100;
        double uptime30Days = (double)lst30Days.Count(item => item.Status) / lst30Days.Count * 100;
        double uptime3Months = (double)lst3Months.Count(item => item.Status) / lst3Months.Count * 100;
        double uptime6Months = (double)lst6Months.Count(item => item.Status) / lst6Months.Count * 100;

        var monitorDashboard = new MonitorDashboard
        {
            ResponseTime = result.Average(x => x.ResponseTime),
            Uptime24Hrs = uptime24Hrs,
            Uptime7Days = upTime7Days,
            Uptime30Days = uptime30Days,
            Uptime3Months = uptime3Months,
            Uptime6Months = uptime6Months,
            CertExpDays = monitor.DaysToExpireCert,
            MonitorId = id
        };
        return monitorDashboard;
    }

    public async Task SetMonitorDashboardDataCacheList()
    {
        try
        {
            Console.WriteLine("Started SetMonitorDashboardDataCacheList");
            var sw = new Stopwatch();
            var lstMonitorDashboard = new List<MonitorDashboard>();
            var lstMonitor = await GetMonitorList();
            Console.WriteLine(lstMonitor.Count());
            foreach (var monitor in lstMonitor)
            {
                var monitorData = await GetMonitorDashboardData(monitor.Id);
                lstMonitorDashboard.Add(monitorData);
            }

            await _caching.SetValueToCacheAsync(_cacheKeyDashboardList, lstMonitorDashboard, 20);
            Console.WriteLine($"Ended SetMonitorDashboardDataCacheList {sw.Elapsed}");
            sw.Stop();
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            throw;
        }
    }

    public IEnumerable<MonitorDashboard> GetMonitorDashboardDataList(List<int> ids)
    {
        var data = _caching.GetValueFromCache<List<MonitorDashboard>>(_cacheKeyDashboardList);

        var dataToReturn = data.Where(x => ids.Contains(x.MonitorId)).ToList();
        return dataToReturn;
    }
}