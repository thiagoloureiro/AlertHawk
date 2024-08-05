using AlertHawk.Authentication.Domain.Entities;

namespace AlertHawk.Application.Interfaces;

public interface IUserActionService
{
    Task CreateAsync(UserAction userAction);

    Task<IEnumerable<UserAction>> GetAsync();
}