using System.Net.Http.Headers;
using AlertHawk.Authentication.Domain.Entities;
using AlertHawk.Monitoring.Domain.Entities;
using AlertHawk.Monitoring.Domain.Interfaces.Repositories;
using AlertHawk.Monitoring.Domain.Interfaces.Services;
using Newtonsoft.Json;

namespace AlertHawk.Monitoring.Domain.Classes;

public class MonitorGroupService : IMonitorGroupService
{
    private readonly IMonitorGroupRepository _monitorGroupRepository;

    public MonitorGroupService(IMonitorGroupRepository monitorGroupRepository)
    {
        _monitorGroupRepository = monitorGroupRepository;
    }

    public async Task<IEnumerable<MonitorGroup>> GetMonitorGroupList()
    {
        var monitorGroupList = await _monitorGroupRepository.GetMonitorGroupList();
        return monitorGroupList;
    }

    public async Task<IEnumerable<MonitorGroup>> GetMonitorGroupList(string jwtToken, MonitorEnvironment environment)
    {
        var ids = await GetUserGroupMonitorListIds(jwtToken);
        var monitorGroupList = await _monitorGroupRepository.GetMonitorGroupList(environment);

        if (ids == null)
        {
            return new List<MonitorGroup> { new MonitorGroup { Id = 0, Name = "No Groups Found" } };
        }

        monitorGroupList = monitorGroupList.Where(x => ids.Contains(x.Id));
        return monitorGroupList;
    }

    public async Task<MonitorGroup> GetMonitorGroupById(int id)
    {
        return await _monitorGroupRepository.GetMonitorGroupById(id);
    }

    public async Task AddMonitorToGroup(MonitorGroupItems monitorGroupItems)
    {
        await _monitorGroupRepository.AddMonitorToGroup(monitorGroupItems);
    }

    public async Task RemoveMonitorFromGroup(MonitorGroupItems monitorGroupItems)
    {
        await _monitorGroupRepository.RemoveMonitorFromGroup(monitorGroupItems);
    }

    public async Task AddMonitorGroup(MonitorGroup monitorGroup)
    {
        await _monitorGroupRepository.AddMonitorGroup(monitorGroup);
    }

    public async Task UpdateMonitorGroup(MonitorGroup monitorGroup)
    {
        await _monitorGroupRepository.UpdateMonitorGroup(monitorGroup);
    }

    public async Task DeleteMonitorGroup(int id)
    {
        await _monitorGroupRepository.DeleteMonitorGroup(id);
    }

    public async Task<List<int>?> GetUserGroupMonitorListIds(string token)
    {
        using var client = new HttpClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var content = await client.GetAsync("https://dev.api.alerthawk.tech/auth/api/UsersMonitorGroup/GetAll");
        var result = await content.Content.ReadAsStringAsync();
        var groupMonitorIds = JsonConvert.DeserializeObject<List<UsersMonitorGroup>>(result);
        var listGroupMonitorIds = groupMonitorIds?.Select(x => x.GroupMonitorId).ToList();
        return listGroupMonitorIds;
    }
}