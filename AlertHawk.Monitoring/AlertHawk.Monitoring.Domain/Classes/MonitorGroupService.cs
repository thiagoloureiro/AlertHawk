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
    private readonly string _cacheKeyMonitorGroupList = "monitorGroupList_";

    public MonitorGroupService(IMonitorGroupRepository monitorGroupRepository, ICaching caching)
    {
        _monitorGroupRepository = monitorGroupRepository;
        _caching = caching;
    }

    public async Task<IEnumerable<MonitorGroup>> GetMonitorGroupList()
    {
        var monitorGroupList = await _monitorGroupRepository.GetMonitorGroupList();
        return monitorGroupList;
    }

    public async Task<IEnumerable<MonitorGroup>> GetMonitorGroupListByEnvironment(string jwtToken,
        MonitorEnvironment environment)
    {
        var ids = await GetUserGroupMonitorListIds(jwtToken);

        var monitorGroupList = await _caching.GetOrSetObjectFromCacheAsync($"{_cacheKeyMonitorGroupList}{(int)environment}",
            20,
            () => _monitorGroupRepository.GetMonitorGroupListByEnvironment(environment));

        if (ids == null)
        {
            return new List<MonitorGroup> { new MonitorGroup { Id = 0, Name = "No Groups Found" } };
        }

        monitorGroupList = monitorGroupList.Where(x => ids.Contains(x.Id));

        var monitorGroups = monitorGroupList.ToList();
        foreach (var monitorGroup in monitorGroups)
        {
            if (monitorGroup.Monitors != null)
            {
                var dashboardData =
                    GetMonitorDashboardDataList(monitorGroup.Monitors.Select(x => x.Id).ToList());
                foreach (var monitor in monitorGroup.Monitors)
                {
                    monitor.MonitorStatusDashboard = dashboardData.FirstOrDefault(x => x.MonitorId == monitor.Id);
                }
            }
        }

        return monitorGroups;
    }

    public async Task<IEnumerable<MonitorGroup>> GetMonitorGroupList(string jwtToken)
    {
        var ids = await GetUserGroupMonitorListIds(jwtToken);
        var monitorGroupList = await _monitorGroupRepository.GetMonitorGroupList();

        if (ids == null)
        {
            return new List<MonitorGroup> { new MonitorGroup { Id = 0, Name = "No Groups Found" } };
        }

        monitorGroupList = monitorGroupList.Where(x => ids.Contains(x.Id));

        var monitorGroups = monitorGroupList.ToList();
        foreach (var monitorGroup in monitorGroups)
        {
            if (monitorGroup.Monitors != null)
            {
                var dashboardData =
                    GetMonitorDashboardDataList(monitorGroup.Monitors.Select(x => x.Id).ToList());
                foreach (var monitor in monitorGroup.Monitors)
                {
                    monitor.MonitorStatusDashboard = dashboardData.FirstOrDefault(x => x.MonitorId == monitor.Id);
                }
            }
        }

        return monitorGroups;
    }

    public async Task<MonitorGroup> GetMonitorGroupById(int id)
    {
        return await _monitorGroupRepository.GetMonitorGroupById(id);
    }

    public async Task AddMonitorToGroup(MonitorGroupItems monitorGroupItems)
    {
        InvalidateGroupCache();
        await _monitorGroupRepository.AddMonitorToGroup(monitorGroupItems);
    }

    public async Task RemoveMonitorFromGroup(MonitorGroupItems monitorGroupItems)
    {
        InvalidateGroupCache();
        await _monitorGroupRepository.RemoveMonitorFromGroup(monitorGroupItems);
    }

    public async Task AddMonitorGroup(MonitorGroup monitorGroup)
    {
        InvalidateGroupCache();
        await _monitorGroupRepository.AddMonitorGroup(monitorGroup);
    }

    public async Task UpdateMonitorGroup(MonitorGroup monitorGroup)
    {
        InvalidateGroupCache();
        await _monitorGroupRepository.UpdateMonitorGroup(monitorGroup);
    }

    public async Task DeleteMonitorGroup(int id)
    {
        InvalidateGroupCache();
        await _monitorGroupRepository.DeleteMonitorGroup(id);
    }

    private void InvalidateGroupCache()
    {
        _caching.Invalidate(_cacheKeyMonitorGroupList + (int)MonitorEnvironment.Production);
        _caching.Invalidate(_cacheKeyMonitorGroupList + (int)MonitorEnvironment.Development);
        _caching.Invalidate(_cacheKeyMonitorGroupList + (int)MonitorEnvironment.QA);
        _caching.Invalidate(_cacheKeyMonitorGroupList + (int)MonitorEnvironment.Staging);
        _caching.Invalidate(_cacheKeyMonitorGroupList + (int)MonitorEnvironment.Testing);
        _caching.Invalidate(_cacheKeyMonitorGroupList + (int)MonitorEnvironment.PreProd);
    }

    public async Task<List<int>?> GetUserGroupMonitorListIds(string token)
    {
        using var client = new HttpClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var authApi = Environment.GetEnvironmentVariable("AUTH_API_URL") ?? "https://dev.api.alerthawk.tech/auth/";
        var content = await client.GetAsync($"{authApi}api/UsersMonitorGroup/GetAll");
        var result = await content.Content.ReadAsStringAsync();
        var groupMonitorIds = JsonConvert.DeserializeObject<List<UsersMonitorGroup>>(result);
        var listGroupMonitorIds = groupMonitorIds?.Select(x => x.GroupMonitorId).ToList();
        return listGroupMonitorIds;
    }

    private IEnumerable<MonitorDashboard> GetMonitorDashboardDataList(List<int> ids)
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