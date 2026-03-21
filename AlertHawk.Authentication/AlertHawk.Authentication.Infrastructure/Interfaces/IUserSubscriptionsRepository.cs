using AlertHawk.Authentication.Domain.Entities;

namespace AlertHawk.Authentication.Infrastructure.Interfaces;

public interface IUserSubscriptionsRepository
{
    Task EnsureTableExistsAsync();

    Task CreateAsync(UserSubscriptions userSubscription);

    Task DeleteAllByUserIdAsync(Guid userId);

    Task<IEnumerable<UserSubscriptions>> GetByUserIdAsync(Guid userId);
}
