using AlertHawk.Authentication.Domain.Entities;

namespace AlertHawk.Authentication.Infrastructure.Interfaces;

public interface IUserActionRepository
{
    Task CreateAsync(UserAction userAction);
    Task<IEnumerable<UserAction>> GetAsync();
}