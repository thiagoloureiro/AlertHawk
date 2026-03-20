using AlertHawk.Authentication.Domain.Entities;

namespace AlertHawk.Application.Interfaces;

public interface IUserSubscriptionsService
{
    Task CreateAsync(UserSubscriptions userSubscription);

    Task CreateOrUpdateAsync(Guid userId, IReadOnlyList<UserSubscriptions> subscriptions);

    Task DeleteAllByUserIdAsync(Guid userId);

    Task<IEnumerable<UserSubscriptions>> GetByUserIdAsync(Guid userId);
}
