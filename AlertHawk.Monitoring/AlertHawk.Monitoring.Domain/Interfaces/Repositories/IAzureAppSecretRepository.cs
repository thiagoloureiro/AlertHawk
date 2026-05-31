using AlertHawk.Monitoring.Domain.Entities;

namespace AlertHawk.Monitoring.Domain.Interfaces.Repositories;

public interface IAzureAppSecretRepository
{
    Task UpsertAsync(AzureAppSecret secret);

    Task<IEnumerable<AzureAppSecret>> GetAllAsync();

    Task InitializeTableIfNotExists();

    Task DeleteExceptApplicationObjectIdsAsync(IEnumerable<string> applicationObjectIds);

    Task DeleteByApplicationObjectIdAsync(string applicationObjectId);
}
