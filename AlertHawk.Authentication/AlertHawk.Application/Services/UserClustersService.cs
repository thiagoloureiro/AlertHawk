using AlertHawk.Application.Interfaces;
using AlertHawk.Authentication.Domain.Entities;
using AlertHawk.Authentication.Infrastructure.Interfaces;

namespace AlertHawk.Application.Services;

public class UserClustersService(IUserClustersRepository repository) : IUserClustersService
{
    public async Task CreateAsync(UserClusters userCluster)
    {
        await repository.CreateAsync(userCluster);
    }

    public async Task CreateOrUpdateAsync(Guid userId, List<string> clusterNames)
    {
        // Delete existing clusters for the user
        await DeleteAllByUserIdAsync(userId);
        
        // Add Clusters for the user
        foreach (var clusterName in clusterNames)
        {
            if (!string.IsNullOrWhiteSpace(clusterName))
            {
                var userCluster = new UserClusters
                {
                    UserId = userId,
                    ClusterName = clusterName
                };
                await repository.CreateAsync(userCluster);
            }
        }
    }

    public async Task DeleteAllByUserIdAsync(Guid userId)
    {
        await repository.DeleteAllByUserIdAsync(userId);
    }

    public async Task<IEnumerable<UserClusters>> GetByUserIdAsync(Guid userId)
    {
        return await repository.GetByUserIdAsync(userId);
    }
}

