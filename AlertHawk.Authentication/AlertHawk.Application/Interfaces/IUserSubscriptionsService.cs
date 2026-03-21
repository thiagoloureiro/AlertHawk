using AlertHawk.Authentication.Domain.Entities;

namespace AlertHawk.Application.Interfaces;

public interface IUserSubscriptionsService
{
    Task CreateAsync(UserSubscriptions userSubscription);

    Task CreateOrUpdateAsync(Guid userId, List<Guid> subscriptionIds);

    Task DeleteAllByUserIdAsync(Guid userId);

    Task<IEnumerable<UserSubscriptions>> GetByUserIdAsync(Guid userId);
}
