using System.Net.Http.Headers;
using AlertHawk.Authentication.Domain.Entities;
using AlertHawk.Monitoring.Domain.Entities;
using AlertHawk.Monitoring.Domain.Interfaces.Repositories;
using AlertHawk.Monitoring.Domain.Interfaces.Services;
using EasyMemoryCache;
using Newtonsoft.Json;

namespace AlertHawk.Monitoring.Domain.Classes;

public class MonitorGroupService : IMonitorGroupService
{
    private readonly IMonitorGroupRepository _monitorGroupRepository;
    private readonly ICaching _caching;
    private readonly string _cacheKeyDashboardList = "MonitorDashboardList";
    private readonly string _cacheKeyMonitorGroupList = "MonitorGroupList";
    private readonly string _cacheKeyMonitorDayHist = "CacheKeyMonitorDayHist_";
    private readonly IMonitorRepository _monitorRepository;
    private readonly IHttpClientFactory _httpClientFactory;

    public MonitorGroupService(IMonitorGroupRepository monitorGroupRepository, ICaching caching,
        IMonitorRepository monitorRepository, IHttpClientFactory httpClientFactory)
    {
        _monitorGroupRepository = monitorGroupRepository;
        _caching = caching;
        _monitorRepository = monitorRepository;
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
    }

    public async Task<IEnumerable<MonitorGroup>> GetMonitorGroupList()
    {
        var monitorGroupList = await _caching.GetOrSetObjectFromCacheAsync(_cacheKeyMonitorGroupList, 10,
            () => _monitorGroupRepository.GetMonitorGroupList());
        return monitorGroupList;
    }

    public async Task<IEnumerable<MonitorGroup>> GetMonitorGroupListByEnvironment(string jwtToken,
        MonitorEnvironment environment)
    {
        var ids = await GetUserGroupMonitorListIds(jwtToken);
        var monitorGroupList = await _monitorGroupRepository.GetMonitorGroupListByEnvironment(environment);

        if (ids == null)
        {
            return new List<MonitorGroup> { new MonitorGroup { Id = 0, Name = "No Groups Found" } };
        }

        monitorGroupList = monitorGroupList.Where(x => ids.Contains(x.Id));
        monitorGroupList = monitorGroupList.Where(x => x.Monitors.Any());

        var monitorGroups = monitorGroupList.ToList();

        var allMonitorIds = monitorGroupList
            .SelectMany(group => group.Monitors?.Select(m => m.Id) ?? Enumerable.Empty<int>()).ToList();
        var allDashboardData = await GetMonitorDashboardDataList(allMonitorIds);
        var monitorDashboards = allDashboardData.ToList();

        foreach (var monitorGroup in monitorGroups)
        {
            if (monitorGroup.Monitors != null && monitorGroup.Monitors.Any())
            {
                monitorGroup.Monitors = monitorGroup.Monitors.OrderBy(x => x.Name).ToList();
                foreach (var monitor in monitorGroup.Monitors)
                {
                    var dashboardData = monitorDashboards.Find(x => x.MonitorId == monitor.Id);
                    monitor.MonitorStatusDashboard = dashboardData;

                    if (monitor.MonitorStatusDashboard == null)
                    {
                        monitor.MonitorStatusDashboard = new MonitorDashboard
                        {
                            MonitorId = monitor.Id,
                            Uptime1Hr = 0,
                            Uptime6Months = 0,
                            Uptime7Days = 0,
                            Uptime3Months = 0,
                            Uptime30Days = 0,
                            Uptime24Hrs = 0,
                            CertExpDays = 0,
                            ResponseTime = 0
                        };
                    }

                    var data = await _caching.GetOrSetObjectFromCacheAsync(_cacheKeyMonitorDayHist + monitor.Id, 10,
                        () =>
                            _monitorRepository.GetMonitorHistoryByIdAndHours(monitor.Id, 1));

                    monitor.MonitorStatusDashboard.HistoryData = data;
                }

                var avg1hr = monitorGroup.Monitors
                    .Select(x => x.MonitorStatusDashboard?.Uptime1Hr)
                    .Where(x => x > 0) // Filter out values greater than zero
                    .Average();

                var avg24hrs = monitorGroup.Monitors
                    .Select(x => x.MonitorStatusDashboard?.Uptime24Hrs)
                    .Where(x => x > 0) // Filter out values greater than zero
                    .Average();

                var avg7days = monitorGroup.Monitors
                    .Select(x => x.MonitorStatusDashboard?.Uptime7Days)
                    .Where(x => x > 0) // Filter out values greater than zero
                    .Average();

                var avg30days = monitorGroup.Monitors
                    .Select(x => x.MonitorStatusDashboard?.Uptime30Days)
                    .Where(x => x > 0) // Filter out values greater than zero
                    .Average();

                var avg3months = monitorGroup.Monitors
                    .Select(x => x.MonitorStatusDashboard?.Uptime3Months)
                    .Where(x => x > 0) // Filter out values greater than zero
                    .Average();

                var avg6months = monitorGroup.Monitors
                    .Select(x => x.MonitorStatusDashboard?.Uptime6Months)
                    .Where(x => x > 0) // Filter out values greater than zero
                    .Average();

                monitorGroup.AvgUptime1Hr = avg1hr;
                monitorGroup.AvgUptime24Hrs = avg24hrs;
                monitorGroup.AvgUptime7Days = avg7days;
                monitorGroup.AvgUptime30Days = avg30days;
                monitorGroup.AvgUptime3Months = avg3months;
                monitorGroup.AvgUptime6Months = avg6months;
            }
        }

        return monitorGroups;
    }

    public async Task<IEnumerable<MonitorGroup>> GetMonitorGroupList(string jwtToken)
    {
        var ids = await GetUserGroupMonitorListIds(jwtToken);
        var monitorGroupList = await _monitorGroupRepository.GetMonitorGroupList();

        if (ids == null || !ids.Any())
        {
            return new List<MonitorGroup> { new MonitorGroup { Id = 0, Name = "No Groups Found" } };
        }

        monitorGroupList = monitorGroupList.Where(x => ids.Contains(x.Id)).ToList();

        return monitorGroupList;
    }
    
    public async Task<IEnumerable<MonitorGroup>> GetMonitorDashboardGroupListByUser(string jwtToken)
    {
        var ids = await GetUserGroupMonitorListIds(jwtToken);
        var monitorGroupList = await _monitorGroupRepository.GetMonitorGroupList();

        if (ids == null || !ids.Any())
        {
            return new List<MonitorGroup> { new MonitorGroup { Id = 0, Name = "No Groups Found" } };
        }

        monitorGroupList = monitorGroupList.Where(x => ids.Contains(x.Id)).ToList();

        var allMonitorIds = monitorGroupList
            .SelectMany(group => group.Monitors?.Select(m => m.Id) ?? Enumerable.Empty<int>()).ToList();
        var allDashboardData = await GetMonitorDashboardDataList(allMonitorIds);
        var monitorDashboards = allDashboardData.ToList();

        foreach (var monitorGroup in monitorGroupList)
        {
            if (monitorGroup.Monitors != null)
            {
                foreach (var monitor in monitorGroup.Monitors)
                {
                    var dashboardData = monitorDashboards.Find(x => x.MonitorId == monitor.Id);
                    monitor.MonitorStatusDashboard = dashboardData ?? new MonitorDashboard
                    {
                        MonitorId = monitor.Id,
                        Uptime1Hr = 0,
                        Uptime6Months = 0,
                        Uptime7Days = 0,
                        Uptime3Months = 0,
                        Uptime30Days = 0,
                        Uptime24Hrs = 0,
                        CertExpDays = 0,
                        ResponseTime = 0
                    };
                }
            }
        }

        return monitorGroupList;
    }

    public async Task<MonitorGroup> GetMonitorGroupByName(string monitorGroupName)
    {
        return await _monitorGroupRepository.GetMonitorGroupByName(monitorGroupName);
    }

    public async Task<MonitorGroup> GetMonitorGroupById(int id)
    {
        return await _monitorGroupRepository.GetMonitorGroupById(id);
    }

    public async Task AddMonitorToGroup(MonitorGroupItems monitorGroupItems)
    {
        await _monitorGroupRepository.AddMonitorToGroup(monitorGroupItems);
        _caching.Invalidate(_cacheKeyMonitorGroupList);

    }

    public async Task RemoveMonitorFromGroup(MonitorGroupItems monitorGroupItems)
    {
        await _monitorGroupRepository.RemoveMonitorFromGroup(monitorGroupItems);
        _caching.Invalidate(_cacheKeyMonitorGroupList);

    }

    public async Task AddMonitorGroup(MonitorGroup monitorGroup)
    {
        await _monitorGroupRepository.AddMonitorGroup(monitorGroup);
        _caching.Invalidate(_cacheKeyMonitorGroupList);
    }

    public async Task UpdateMonitorGroup(MonitorGroup monitorGroup)
    {
        
        await _monitorGroupRepository.UpdateMonitorGroup(monitorGroup);
        _caching.Invalidate(_cacheKeyMonitorGroupList);
    }

    public async Task DeleteMonitorGroup(string jwtToken, int id)
    {
        await DeleteUserGroupMonitorListIds(jwtToken, id);
        await _monitorGroupRepository.DeleteMonitorGroup(id);
        _caching.Invalidate(_cacheKeyMonitorGroupList);
    }

    public async Task<List<int>?> GetUserGroupMonitorListIds(string token)
    {
        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Add("User-Agent", "AlertHawk/1.0.1");
        client.DefaultRequestHeaders.Add("Accept-Encoding", "br");
        client.DefaultRequestHeaders.Add("Connection", "keep-alive");
        client.DefaultRequestHeaders.Add("Accept", "*/*");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var authApi = Environment.GetEnvironmentVariable("AUTH_API_URL") ??
                      "https://api.monitoring.electrificationtools.abb.com/auth/";
        var content = await client.GetAsync($"{authApi}api/UsersMonitorGroup/GetAll");
        var result = await content.Content.ReadAsStringAsync();
        
        // Check if the response is empty
        if (string.IsNullOrEmpty(result))
        {
            return new List<int>();
        }
        
        var groupMonitorIds = JsonConvert.DeserializeObject<List<UsersMonitorGroup>>(result);
        var listGroupMonitorIds = groupMonitorIds?.Select(x => x.GroupMonitorId).ToList();
        return listGroupMonitorIds;
    }
    public async Task DeleteUserGroupMonitorListIds(string token, int userGroupMonitorId)
    {
        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Add("User-Agent", "AlertHawk/1.0.1");
        client.DefaultRequestHeaders.Add("Accept-Encoding", "br");
        client.DefaultRequestHeaders.Add("Connection", "keep-alive");
        client.DefaultRequestHeaders.Add("Accept", "*/*");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var authApi = Environment.GetEnvironmentVariable("AUTH_API_URL") ??
                      "https://api.monitoring.electrificationtools.abb.com/auth/";
        await client.DeleteAsync($"{authApi}api/UsersMonitorGroup/{userGroupMonitorId}");
    }

    private async Task<IEnumerable<MonitorDashboard>> GetMonitorDashboardDataList(List<int> ids)
    {
        var data = await _caching.GetValueFromCacheAsync<List<MonitorDashboard?>>(_cacheKeyDashboardList);

        if (data != null)
        {
            var items = data.Where(item => item != null).ToList();

            IEnumerable<MonitorDashboard> dataToReturn = items.Where(x => ids.Contains(x.MonitorId)).ToList();

            return dataToReturn;
        }

        return new List<MonitorDashboard>();
    }
}