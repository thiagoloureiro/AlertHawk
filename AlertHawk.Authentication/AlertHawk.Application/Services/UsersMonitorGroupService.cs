using AlertHawk.Application.Interfaces;
using AlertHawk.Authentication.Domain.Entities;
using AlertHawk.Authentication.Infrastructure.Interfaces;

namespace AlertHawk.Application.Services;

public class UsersMonitorGroupService(IUsersMonitorGroupRepository _repository) : IUsersMonitorGroupService
{
    public async Task CreateOrUpdateAsync(List<UsersMonitorGroup> usersMonitorGroup)
    {
        await DeleteAllByUserIdAsync(usersMonitorGroup!.FirstOrDefault()!.UserId);
        foreach (var item in usersMonitorGroup)
        {
            if (item.GroupMonitorId > 0)
            {
                item.Id = Guid.NewGuid();
                await _repository.CreateAsync(item);
            }
        }
    }

    public async Task DeleteAllByUserIdAsync(Guid userId) => await _repository.DeleteAllByUserIdAsync(userId);

    public async Task<IEnumerable<UsersMonitorGroup>> GetAsync(Guid userId) => await _repository.GetAsync(userId);
    public async Task DeleteAllByGroupMonitorIdAsync(int groupId)
    {
        await _repository.DeleteAllByGroupMonitorIdAsync(groupId);
    }
}