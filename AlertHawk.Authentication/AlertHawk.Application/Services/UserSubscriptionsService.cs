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

    public async Task CreateOrUpdateAsync(Guid userId, IReadOnlyList<UserSubscriptions> subscriptions)
    {
        await DeleteAllByUserIdAsync(userId);

        foreach (var row in subscriptions)
        {
            if (row.SubscriptionId == Guid.Empty)
            {
                continue;
            }

            var userSubscription = new UserSubscriptions
            {
                UserId = userId,
                SubscriptionId = row.SubscriptionId,
                SubscriptionName = row.SubscriptionName ?? string.Empty
            };
            await repository.CreateAsync(userSubscription);
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
