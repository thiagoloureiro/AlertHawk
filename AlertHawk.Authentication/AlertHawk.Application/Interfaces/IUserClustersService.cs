using AlertHawk.Authentication.Domain.Entities;

namespace AlertHawk.Application.Interfaces;

public interface IUserClustersService
{
    Task CreateAsync(UserClusters userCluster);
    
    Task CreateOrUpdateAsync(Guid userId, List<string> clusterNames);
    
    Task DeleteAllByUserIdAsync(Guid userId);
    
    Task<IEnumerable<UserClusters>> GetByUserIdAsync(Guid userId);
}

