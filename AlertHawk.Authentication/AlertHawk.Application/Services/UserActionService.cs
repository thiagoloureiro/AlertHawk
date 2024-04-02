using AlertHawk.Application.Interfaces;
using AlertHawk.Authentication.Domain.Entities;
using AlertHawk.Authentication.Infrastructure.Interfaces;

namespace AlertHawk.Application.Services;

public class UserActionService : IUserActionService
{
    private readonly IUserActionRepository _userActionRepository;

    public UserActionService(IUserActionRepository userActionRepository)
    {
        _userActionRepository = userActionRepository;
    }

    public async Task CreateAsync(UserAction userAction)
    {
        await _userActionRepository.CreateAsync(userAction);
    }

    public async Task<IEnumerable<UserAction>> GetAsync()
    {
        return await _userActionRepository.GetAsync();
    }
}