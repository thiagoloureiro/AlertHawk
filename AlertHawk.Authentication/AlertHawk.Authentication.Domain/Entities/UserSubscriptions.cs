using System.Diagnostics.CodeAnalysis;

namespace AlertHawk.Authentication.Domain.Entities;

[ExcludeFromCodeCoverage]
public class UserSubscriptions
{
    public Guid UserId { get; set; }

    public Guid SubscriptionId { get; set; }

    public string SubscriptionName { get; set; }
}
