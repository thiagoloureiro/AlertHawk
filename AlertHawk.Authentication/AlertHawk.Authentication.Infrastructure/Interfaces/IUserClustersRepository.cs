using AlertHawk.Authentication.Domain.Entities;

namespace AlertHawk.Authentication.Infrastructure.Interfaces;

public interface IUserClustersRepository
{
    Task EnsureTableExistsAsync();
    
    Task CreateAsync(UserClusters userCluster);
    
    Task DeleteAllByUserIdAsync(Guid userId);
    
    Task<IEnumerable<UserClusters>> GetByUserIdAsync(Guid userId);
}

