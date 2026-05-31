using AlertHawk.Monitoring.Domain.Entities;

namespace AlertHawk.Monitoring.Domain.Interfaces.Repositories;

public interface IAzureAppRegistrationWatchRepository
{
    Task InitializeTableIfNotExists();

    Task<IEnumerable<AzureAppRegistrationWatch>> GetAllAsync(bool enabledOnly = true);

    Task<AzureAppRegistrationWatch?> GetByObjectIdAsync(string applicationObjectId);

    Task<int> AddAsync(AzureAppRegistrationWatch watch);

    Task DeleteAsync(int id);

    Task DeleteAsync(string applicationObjectId);
}
