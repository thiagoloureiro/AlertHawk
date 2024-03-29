using AlertHawk.Monitoring.Domain.Entities;
using AlertHawk.Monitoring.Domain.Interfaces.Repositories;
using AlertHawk.Monitoring.Domain.Interfaces.Services;
using AlertHawk.Monitoring.Domain.Utils;
using EasyMemoryCache;
using Monitor = AlertHawk.Monitoring.Domain.Entities.Monitor;

namespace AlertHawk.Monitoring.Domain.Classes;

public class MonitorService : IMonitorService
{
    private readonly IMonitorRepository _monitorRepository;
    private readonly ICaching _caching;
    private readonly string _cacheKeyDashboardList = "MonitorDashboardList";
    private readonly IMonitorGroupService _monitorGroupService;

    public MonitorService(IMonitorRepository monitorRepository, ICaching caching,
        IMonitorGroupService monitorGroupService)
    {
        _monitorRepository = monitorRepository;
        _caching = caching;
        _monitorGroupService = monitorGroupService;
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
        return await _monitorRepository.GetMonitorHistoryByIdAndDays(id, days);
    }

    public async Task<IEnumerable<Monitor?>> GetMonitorList()
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

    public async Task<MonitorDashboard?> GetMonitorDashboardData(int id)
    {
        try
        {
            Console.WriteLine($"Caching {id}");
            var result = await _caching.GetOrSetObjectFromCacheAsync($"GroupHistory_{id}_90", 10,
                () => _monitorRepository.GetMonitorHistoryByIdAndDays(id, 90));

            if (result == null)
            {
                return new MonitorDashboard
                {
                    ResponseTime = 0,
                    MonitorId = id,
                    Uptime1Hr = 0,
                    Uptime3Months = 0,
                    Uptime6Months = 0,
                    Uptime24Hrs = 0,
                    Uptime30Days = 0,
                    CertExpDays = 0,
                    Uptime7Days = 0
                };
            }

            var monitor =
                await _caching.GetOrSetObjectFromCacheAsync($"Monitor_{id}", 5,
                    () => _monitorRepository.GetMonitorById(id));

            if (monitor == null)
            {
                return new MonitorDashboard
                {
                    ResponseTime = 0,
                    MonitorId = id,
                    Uptime1Hr = 0,
                    Uptime3Months = 0,
                    Uptime6Months = 0,
                    Uptime24Hrs = 0,
                    Uptime30Days = 0,
                    CertExpDays = 0,
                    Uptime7Days = 0
                };
            }

            var monitorHistories = result.ToList();
            var lst1Hr = monitorHistories.Where(x => x.TimeStamp > DateTime.Now.AddHours(-1)).ToList();
            var lst24Hrs = monitorHistories.Where(x => x.TimeStamp > DateTime.Now.AddDays(-1)).ToList();
            var lst7Days = monitorHistories.Where(x => x.TimeStamp > DateTime.Now.AddDays(-7)).ToList();
            var lst30Days = monitorHistories.Where(x => x.TimeStamp > DateTime.Now.AddDays(-30)).ToList();
            var lst3Months = monitorHistories.Where(x => x.TimeStamp > DateTime.Now.AddDays(-90)).ToList();
            var lst6Months = monitorHistories.Where(x => x.TimeStamp > DateTime.Now.AddDays(-180)).ToList();

            // Check if last 24 hours data is present
            bool containsLast24HoursData = false;
            if (lst24Hrs.Any())
            {
                containsLast24HoursData =
                    lst24Hrs.Min(x => x.TimeStamp) <= DateTime.Now.AddDays(-1).AddSeconds(120);
            }

            // Check if last 7 days data is present
            bool containsLast7DaysData = false;
            if (lst7Days.Any())
            {
                containsLast7DaysData =
                    lst7Days.Min(x => x.TimeStamp) <= DateTime.Now.AddDays(-7).AddSeconds(120);
            }

            // Check if last 30 days data is present
            bool containsLast30DaysData = false;
            if (lst30Days.Any())
            {
                containsLast30DaysData =
                    lst30Days.Min(x => x.TimeStamp) <= DateTime.Now.AddDays(-30).AddSeconds(120);
            }


            // Check if last 3 months data is present
            bool containsLast3MonthsData = false;
            if (lst3Months.Any())
            {
                containsLast3MonthsData =
                    lst3Months.Min(x => x.TimeStamp) <= DateTime.Now.AddDays(-90).AddSeconds(120);
            }

            // Check if last 6 months data is present
            bool containsLast6MonthsData = false;
            if (lst6Months.Any())
            {
                containsLast6MonthsData =
                    lst6Months.Min(x => x.TimeStamp) <= DateTime.Now.AddDays(-180).AddSeconds(120);
            }

            double uptime1Hr = 0.0;
            if (lst1Hr.Count > 0)
            {
                uptime1Hr = (double)lst1Hr.Count(item => item.Status) / lst1Hr.Count * 100;
            }

            double uptime24Hrs = 0.0;
            if (lst24Hrs.Count > 0 && containsLast24HoursData)
            {
                uptime24Hrs = (double)lst24Hrs.Count(item => item.Status) / lst24Hrs.Count * 100;
            }

            double upTime7Days = 0.0;
            if (lst7Days.Count > 0 && containsLast7DaysData)
            {
                upTime7Days = (double)lst7Days.Count(item => item.Status) / lst7Days.Count * 100;
            }

            double uptime30Days = 0.0;
            if (lst30Days.Count > 0 && containsLast30DaysData)
            {
                uptime30Days = (double)lst30Days.Count(item => item.Status) / lst30Days.Count * 100;
            }

            double uptime3Months = 0.0;
            if (lst3Months.Count > 0 && containsLast3MonthsData)
            {
                uptime3Months = (double)lst3Months.Count(item => item.Status) / lst3Months.Count * 100;
            }

            double uptime6Months = 0.0;
            if (lst6Months.Count > 0 && containsLast6MonthsData)
            {
                uptime6Months = (double)lst6Months.Count(item => item.Status) / lst6Months.Count * 100;
            }

            if (monitorHistories.Any())
            {
                var monitorDashboard = new MonitorDashboard
                {
                    ResponseTime = Math.Round(monitorHistories.Average(x => x.ResponseTime), 2),
                    Uptime1Hr = Math.Round(uptime1Hr, 2),
                    Uptime24Hrs = Math.Round(uptime24Hrs, 2),
                    Uptime7Days = Math.Round(upTime7Days, 2),
                    Uptime30Days = Math.Round(uptime30Days, 2),
                    Uptime3Months = Math.Round(uptime3Months, 2),
                    Uptime6Months = Math.Round(uptime6Months, 2),
                    CertExpDays = monitor.DaysToExpireCert,
                    MonitorId = id
                };
                return monitorDashboard;
            }

            return new MonitorDashboard
            {
                ResponseTime = 0,
                MonitorId = id,
                Uptime1Hr = 0,
                Uptime3Months = 0,
                Uptime6Months = 0,
                Uptime24Hrs = 0,
                Uptime30Days = 0,
                CertExpDays = 0,
                Uptime7Days = 0
            };
        }
        catch (Exception e)
        {
            SentrySdk.CaptureException(e);
        }

        return new MonitorDashboard
        {
            ResponseTime = 0,
            MonitorId = id,
            Uptime1Hr = 0,
            Uptime3Months = 0,
            Uptime6Months = 0,
            Uptime24Hrs = 0,
            Uptime30Days = 0,
            CertExpDays = 0,
            Uptime7Days = 0
        };
    }

    public async Task SetMonitorDashboardDataCacheList()
    {
        if (GlobalVariables.MasterNode)
        {
            Console.WriteLine("Started Caching Monitor Dashboard Data List");
            var lstMonitorDashboard = new List<MonitorDashboard?>();
            var lstMonitor = await GetMonitorList();

            foreach (var monitor in lstMonitor)
            {
                if (monitor != null)
                {
                    var monitorData = await GetMonitorDashboardData(monitor.Id);
                    lstMonitorDashboard.Add(monitorData);
                }
            }

            await _caching.SetValueToCacheAsync(_cacheKeyDashboardList, lstMonitorDashboard, 20);
            Console.WriteLine("Finished Caching Monitor Dashboard Data List");
        }
    }

    public async Task<MonitorStatusDashboard> GetMonitorStatusDashboard(string jwtToken, MonitorEnvironment environment)
    {
        var monitorList = await GetMonitorListByMonitorGroupIds(jwtToken, environment);

        var enumerable = monitorList.ToList();
        var paused = enumerable.Count(x => x.Paused);

        enumerable.RemoveAll(x => x.Paused);

        var monitorDashboard = new MonitorStatusDashboard
        {
            MonitorUp = enumerable.Count(x => x.Status),
            MonitorDown = enumerable.Count(x => !x.Status),
            MonitorPaused = paused
        };
        return monitorDashboard;
    }

    public async Task<int> CreateMonitorHttp(MonitorHttp monitorHttp)
    {
        if (monitorHttp!.Headers != null)
        {
            monitorHttp.HeadersJson = JsonUtils.ConvertTupleToJson(monitorHttp.Headers);
        }

        return await _monitorRepository.CreateMonitorHttp(monitorHttp);
    }

    public async Task<IEnumerable<MonitorFailureCount>> GetMonitorFailureCount(int days)
    {
        var result = await _monitorRepository.GetMonitorFailureCount(days);
        var monitor = await _monitorRepository.GetMonitorList();
        var monitorList = monitor.ToList();

        var monitorFailureCounts = result.ToList();

        foreach (var item in monitorFailureCounts)
        {
            item.Monitor = monitorList?.FirstOrDefault(x => x.Id == item.MonitorId);
        }

        return monitorFailureCounts;
    }

    public async Task<IEnumerable<Monitor>?> GetMonitorListByMonitorGroupIds(string token,
        MonitorEnvironment environment)
    {
        var listGroupMonitorIds = await _monitorGroupService.GetUserGroupMonitorListIds(token);

        if (listGroupMonitorIds != null)
        {
            var monitors = await _monitorRepository.GetMonitorListByMonitorGroupIds(listGroupMonitorIds, environment);
            if (monitors != null)
            {
                var monitorList = monitors.ToList();
                var dashboardDataList = GetMonitorDashboardDataList(monitorList.Select(x => x.Id).ToList()).ToList();
                if (dashboardDataList.Any())
                {
                    foreach (var monitor in monitorList)
                    {
                        var dashboardData = dashboardDataList.FirstOrDefault(x => x.MonitorId == monitor.Id);
                        if (dashboardData != null)
                        {
                            monitor.MonitorStatusDashboard = dashboardData;
                        }
                    }
                }

                return monitors;
            }
        }

        return null;
    }

    public async Task UpdateMonitorHttp(MonitorHttp monitorHttp)
    {
        if (monitorHttp!.Headers != null)
        {
            monitorHttp.HeadersJson = JsonUtils.ConvertTupleToJson(monitorHttp.Headers);
        }

        await _monitorRepository.UpdateMonitorHttp(monitorHttp);
    }

    public async Task DeleteMonitor(int id)
    {
        await _monitorRepository.DeleteMonitor(id);
    }

    public async Task<int> CreateMonitorTcp(MonitorTcp monitorTcp)
    {
        return await _monitorRepository.CreateMonitorTcp(monitorTcp);
    }

    public async Task UpdateMonitorTcp(MonitorTcp monitorTcp)
    {
        await _monitorRepository.UpdateMonitorTcp(monitorTcp);
    }

    public async Task<MonitorHttp> GetHttpMonitorByMonitorId(int id)
    {
        return await _monitorRepository.GetHttpMonitorByMonitorId(id);
    }

    public async Task<MonitorTcp> GetTcpMonitorByMonitorId(int id)
    {
        return await _monitorRepository.GetTcpMonitorByMonitorId(id);
    }

    public async Task PauseMonitorByGroupId(int groupId, bool paused)
    {
        var monitorGroup = await _monitorGroupService.GetMonitorGroupById(groupId);
        if (monitorGroup != null)
        {
            foreach (var monitor in monitorGroup.Monitors)
            {
                await PauseMonitor(monitor.Id, paused);
            }
        }
    }

    public async Task AddMonitorNotification(MonitorNotification monitorNotification)
    {
        await _monitorRepository.AddMonitorNotification(monitorNotification);
    }

    public async Task RemoveMonitorNotification(MonitorNotification monitorNotification)
    {
        await _monitorRepository.RemoveMonitorNotification(monitorNotification);
    }

    public IEnumerable<MonitorDashboard> GetMonitorDashboardDataList(List<int> ids)
    {
        var data = _caching.GetValueFromCache<List<MonitorDashboard?>>(_cacheKeyDashboardList);

        if (data != null)
        {
            var items = data.Where(item => item != null).ToList();

            var dataToReturn = items.Where(x => ids.Contains(x.MonitorId)).ToList();

            return dataToReturn;
        }

        return new List<MonitorDashboard>();
    }
}