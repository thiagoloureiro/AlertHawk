using AlertHawk.Authentication.Domain.Entities;
using AlertHawk.Monitoring.Domain.Entities;
using AlertHawk.Monitoring.Domain.Interfaces.Repositories;
using AlertHawk.Monitoring.Domain.Interfaces.Services;
using EasyMemoryCache;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Text;
using Monitor = AlertHawk.Monitoring.Domain.Entities.Monitor;

namespace AlertHawk.Monitoring.Domain.Classes;

public class MonitorGroupService : IMonitorGroupService
{
    private readonly IMonitorGroupRepository _monitorGroupRepository;
    private readonly ICaching _caching;
    private readonly string _cacheKeyDashboardList = "MonitorDashboardList";
    private readonly string _cacheKeyMonitorGroupList = "MonitorGroupList";
    private readonly string _cacheKeyMonitorDayHist = "CacheKeyMonitorDayHist_";
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IMonitorHistoryRepository _monitorHistoryRepository;
    private readonly ILogger<MonitorGroupService> _logger;
    private readonly DownSamplingSettings _downSamplingSettings;

    public MonitorGroupService(IMonitorGroupRepository monitorGroupRepository,
                               ICaching caching,
                               IMonitorRepository monitorRepository,
                               IHttpClientFactory httpClientFactory,
                               IMonitorHistoryRepository monitorHistoryRepository,
                               ILogger<MonitorGroupService> logger,
                               IOptions<DownSamplingSettings> options)
    {
        _monitorGroupRepository = monitorGroupRepository;
        _caching = caching;
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _monitorHistoryRepository = monitorHistoryRepository;
        _logger = logger;
        _downSamplingSettings = options.Value;
    }

    public async Task<IEnumerable<MonitorGroup>?> GetMonitorGroupList()
    {
        var monitorGroupList = await _caching.GetOrSetObjectFromCacheAsync(_cacheKeyMonitorGroupList, 10,
            () => _monitorGroupRepository.GetMonitorGroupList());
        return monitorGroupList;
    }

    public async Task<IEnumerable<MonitorGroup>> GetMonitorGroupListByEnvironment(string jwtToken, MonitorEnvironment environment)
    {
        var ids = await GetUserGroupMonitorListIds(jwtToken);

        var monitorGroupList = await _monitorGroupRepository.GetMonitorGroupListByEnvironment(environment);

        if (ids == null)
        {
            return new List<MonitorGroup> { new MonitorGroup { Id = 0, Name = "No Groups Found" } };
        }

        monitorGroupList = monitorGroupList.Where(x => ids.Contains(x.Id));
        monitorGroupList = monitorGroupList.Where(x => x.Monitors != null && x.Monitors.Any());

        var monitorGroups = monitorGroupList.ToList();

        var allMonitorIds = monitorGroupList
            .SelectMany(group => group.Monitors?.Select(m => m.Id) ?? Enumerable.Empty<int>()).ToList();
        var allDashboardData = await GetMonitorDashboardDataList(allMonitorIds);
        var monitorDashboards = allDashboardData.ToList();

        var tasks = new List<Task>();
        var semaphore = new SemaphoreSlim(20); // Limit to 20 concurrent tasks

        foreach (var monitorGroup in monitorGroups)
        {
            if (monitorGroup.Monitors != null && monitorGroup.Monitors.Any())
            {
                monitorGroup.Monitors = monitorGroup.Monitors.OrderBy(x => x.Name).ToList();

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

                    // Create tasks to fetch data asynchronously
                    var task = Task.Run(async () =>
                    {
                        await semaphore.WaitAsync(); // Acquire the semaphore
                        try
                        {
                            await FetchMonitorHistoryAsync(monitor);
                        }
                        finally
                        {
                            semaphore.Release(); // Release the semaphore
                        }
                    });
                    tasks.Add(task);
                }
            }
        }

        // Wait for all tasks to complete
        await Task.WhenAll(tasks);

        // Now process the results
        foreach (var monitorGroup in monitorGroups)
        {
            if (monitorGroup.Monitors != null && monitorGroup.Monitors.Any())
            {
                var avg1hr = monitorGroup.Monitors
                    .Select(x => x.MonitorStatusDashboard?.Uptime1Hr)
                    .Average();

                var avg24hrs = monitorGroup.Monitors
                    .Select(x => x.MonitorStatusDashboard?.Uptime24Hrs)
                    .Average();

                var avg7days = monitorGroup.Monitors
                    .Select(x => x.MonitorStatusDashboard?.Uptime7Days)
                    .Average();

                var avg30days = monitorGroup.Monitors
                    .Select(x => x.MonitorStatusDashboard?.Uptime30Days)
                    .Average();

                var avg3months = monitorGroup.Monitors
                    .Select(x => x.MonitorStatusDashboard?.Uptime3Months)
                    .Where(x => x > 0)
                    .Average();

                var avg6months = monitorGroup.Monitors
                    .Select(x => x.MonitorStatusDashboard?.Uptime6Months)
                    .Where(x => x > 0)
                    .Average();

                if (avg1hr != null)
                {
                    monitorGroup.AvgUptime1Hr = Math.Round((double)avg1hr, 2);
                }

                if (avg24hrs != null)
                {
                    monitorGroup.AvgUptime24Hrs = Math.Round((double)avg24hrs, 2);
                }

                if (avg7days != null)
                {
                    monitorGroup.AvgUptime7Days = Math.Round((double)avg7days, 2);
                }

                if (avg30days != null)
                {
                    monitorGroup.AvgUptime30Days = Math.Round((double)avg30days, 2);
                }

                if (avg3months != null)
                {
                    monitorGroup.AvgUptime3Months = Math.Round((double)avg3months, 2);
                }

                if (avg6months != null)
                {
                    monitorGroup.AvgUptime6Months = Math.Round((double)avg6months, 2);
                }
            }
        }

        async Task FetchMonitorHistoryAsync(Monitor monitor)
        {
            var data = await _caching.GetOrSetObjectFromCacheAsync(_cacheKeyMonitorDayHist + monitor.Id, 10, () => _monitorHistoryRepository.GetMonitorHistoryByIdAndHours(monitor.Id, 1));

            if (monitor.MonitorStatusDashboard is null)
                return;

            monitor.MonitorStatusDashboard.HistoryData = data;

            if (_downSamplingSettings.Active)
            {
                monitor.MonitorStatusDashboard.HistoryData = DownSampleWithStatusCheck(monitor.MonitorStatusDashboard.HistoryData, new TimeSpan(0, 0, _downSamplingSettings.IntervalInSeconds));
            }
        }

        return monitorGroups;
    }

    public async Task<IEnumerable<MonitorGroup>?> GetMonitorGroupList(string jwtToken)
    {
        var ids = await GetUserGroupMonitorListIds(jwtToken);
        var monitorGroupList = await _monitorGroupRepository.GetMonitorGroupList();

        if (ids == null || !ids.Any())
        {
            return null;
        }

        monitorGroupList = monitorGroupList?.Where(x => ids.Contains(x.Id)).ToList();

        return monitorGroupList;
    }

    public async Task<IEnumerable<MonitorGroup>?> GetMonitorDashboardGroupListByUser(string jwtToken)
    {
        var ids = await GetUserGroupMonitorListIds(jwtToken);
        var monitorGroupList = await _monitorGroupRepository.GetMonitorGroupList();

        if (ids == null || !ids.Any())
        {
            return new List<MonitorGroup> { new MonitorGroup { Id = 0, Name = "No Groups Found" } };
        }

        if (monitorGroupList != null)
        {
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

        return null;
    }

    public async Task<MonitorGroup?> GetMonitorGroupByName(string monitorGroupName)
    {
        return await _monitorGroupRepository.GetMonitorGroupByName(monitorGroupName);
    }

    public async Task<IEnumerable<Monitor>?> GetMonitorListByGroupId(int monitorGroupId)
    {
        return await _monitorGroupRepository.GetMonitorListByGroupId(monitorGroupId);
    }

    public async Task<int> GetMonitorGroupIdByMonitorId(int id)
    {
        return await _monitorGroupRepository.GetMonitorGroupIdByMonitorId(id);
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

    public async Task<int> AddMonitorGroup(MonitorGroup monitorGroup, string? jwtToken)
    {
        monitorGroup.Name = monitorGroup.Name.TrimStart();
        monitorGroup.Name = monitorGroup.Name.TrimEnd();

        var groupId = await _monitorGroupRepository.AddMonitorGroup(monitorGroup);

        if (jwtToken != null)
        {
            await AddUserToGroup(jwtToken, groupId);
        }

        _caching.Invalidate(_cacheKeyMonitorGroupList);
        return groupId;
    }

    public async Task UpdateMonitorGroup(MonitorGroup monitorGroup)
    {
        monitorGroup.Name = monitorGroup.Name.TrimStart();
        monitorGroup.Name = monitorGroup.Name.TrimEnd();

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
        var client = CreateHttpClient(token);
        var authApi = Environment.GetEnvironmentVariable("AUTH_API_URL");
        var content = await client.GetAsync($"{authApi}api/UsersMonitorGroup/GetAll");
        var result = await content.Content.ReadAsStringAsync();

        if (!content.IsSuccessStatusCode)
        {
            _logger.LogError($"Error fetching UserGroupMonitorListIds, StatusCode: {content.StatusCode}");
            SentrySdk.CaptureMessage($"Error fetching UserGroupMonitorListIds, StatusCode: {content.StatusCode}");
        }

        // Check if the response is empty
        if (string.IsNullOrEmpty(result))
        {
            return new List<int>();
        }

        var groupMonitorIds = JsonConvert.DeserializeObject<List<UsersMonitorGroup>>(result);
        var listGroupMonitorIds = groupMonitorIds?.Select(x => x.GroupMonitorId).ToList();
        return listGroupMonitorIds;
    }

    public async Task AddUserToGroup(string token, int groupId)
    {
        var client = CreateHttpClient(token);

        var payload = new UsersMonitorGroup { GroupMonitorId = groupId };

        var content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");

        var authApi = Environment.GetEnvironmentVariable("AUTH_API_URL");
        await client.PostAsync($"{authApi}api/UsersMonitorGroup/AssignUserToGroup", content);
    }

    public async Task DeleteUserGroupMonitorListIds(string token, int userGroupMonitorId)
    {
        var client = CreateHttpClient(token);

        var authApi = Environment.GetEnvironmentVariable("AUTH_API_URL");
        await client.DeleteAsync($"{authApi}api/UsersMonitorGroup/{userGroupMonitorId}");
    }

    private HttpClient CreateHttpClient(string token)
    {
        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Add("User-Agent", "AlertHawk/1.0.1");
        client.DefaultRequestHeaders.Add("Accept-Encoding", "br");
        client.DefaultRequestHeaders.Add("Connection", "keep-alive");
        client.DefaultRequestHeaders.Add("Accept", "*/*");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
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

    private static List<MonitorHistory> DownSampleWithStatusCheck(IEnumerable<MonitorHistory> monitorHistories, TimeSpan interval)
    {
        var aggregatedData = new Dictionary<DateTime, List<MonitorHistory>>();

        foreach (var history in monitorHistories)
        {
            var key = new DateTime(history.TimeStamp.Year,
                                   history.TimeStamp.Month,
                                   history.TimeStamp.Day,
                                   history.TimeStamp.Hour,
                                   (int)(history.TimeStamp.Minute / interval.TotalMinutes * (int)interval.TotalMinutes),
                                   0);

            if (!aggregatedData.ContainsKey(key))
            {
                aggregatedData[key] = new List<MonitorHistory>();
            }
            aggregatedData[key].Add(history);
        }

        var result = new List<MonitorHistory>();

        foreach (var group in aggregatedData)
        {
            var histories = group.Value;

            if (histories.Any(h => !h.Status))
            {
                var falseStatusEntry = histories.First(h => !h.Status);
                result.Add(falseStatusEntry);
            }
            else
            {
                result.Add(new MonitorHistory
                {
                    TimeStamp = group.Key,
                    ResponseTime = (int)histories.Average(h => h.ResponseTime),
                    Status = true
                });
            }
        }

        return [.. result.OrderBy(h => h.TimeStamp)];
    }
}