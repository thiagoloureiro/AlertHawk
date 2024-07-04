using AlertHawk.Authentication.Domain.Entities;

namespace AlertHawk.Application.Interfaces;

public interface IUsersMonitorGroupService
{
    Task CreateOrUpdateAsync(List<UsersMonitorGroup> usersMonitorGroup);
    Task DeleteAllByUserIdAsync(Guid userId);
    Task<IEnumerable<UsersMonitorGroup>> GetAsync(Guid userId);
    Task DeleteAllByGroupMonitorIdAsync(int groupId);
    Task AssignUserToGroup(UsersMonitorGroup userMonitorGroup);
}