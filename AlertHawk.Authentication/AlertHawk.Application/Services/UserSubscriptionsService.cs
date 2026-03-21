using AlertHawk.Application.Interfaces;
using AlertHawk.Authentication.Domain.Entities;
using AlertHawk.Authentication.Infrastructure.Interfaces;

namespace AlertHawk.Application.Services;

public class UserSubscriptionsService(IUserSubscriptionsRepository repository) : IUserSubscriptionsService
{
    public async Task CreateAsync(UserSubscriptions userSubscription)
    {
        await repository.CreateAsync(userSubscription);
    }

    public async Task CreateOrUpdateAsync(Guid userId, List<Guid> subscriptionIds)
    {
        await DeleteAllByUserIdAsync(userId);

        foreach (var subscriptionId in subscriptionIds)
        {
            if (subscriptionId != Guid.Empty)
            {
                var userSubscription = new UserSubscriptions
                {
                    UserId = userId,
                    SubscriptionId = subscriptionId
                };
                await repository.CreateAsync(userSubscription);
            }
        }
    }

    public async Task DeleteAllByUserIdAsync(Guid userId)
    {
        await repository.DeleteAllByUserIdAsync(userId);
    }

    public async Task<IEnumerable<UserSubscriptions>> GetByUserIdAsync(Guid userId)
    {
        return await repository.GetByUserIdAsync(userId);
    }
}
