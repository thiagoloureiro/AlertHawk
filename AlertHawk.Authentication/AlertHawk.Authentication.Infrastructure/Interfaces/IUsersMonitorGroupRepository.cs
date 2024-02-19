using AlertHawk.Authentication.Domain.Entities;

namespace AlertHawk.Authentication.Infrastructure.Interfaces;

public interface IUsersMonitorGroupRepository
{
    Task CreateAsync(UsersMonitorGroup usersMonitorGroup);
    Task DeleteAllByUserIdAsync(Guid userId);
    Task<IEnumerable<UsersMonitorGroup>> GetAsync(Guid userId);
}