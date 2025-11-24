using System.Diagnostics.CodeAnalysis;

namespace AlertHawk.Authentication.Domain.Entities;

[ExcludeFromCodeCoverage]
public class UserClusters
{
    public Guid UserId { get; set; }

    public string ClusterName { get; set; }
}