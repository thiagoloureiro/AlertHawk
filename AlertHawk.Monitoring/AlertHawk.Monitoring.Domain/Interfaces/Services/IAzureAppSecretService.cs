using AlertHawk.Monitoring.Domain.Entities;

namespace AlertHawk.Monitoring.Domain.Interfaces.Services;

public interface IAzureAppSecretService
{
    Task<IEnumerable<AzureAppSecret>> GetSecretsAsync(bool expiringOnly = false);

    Task<AzureSecretsStatusSummary> GetStatusAsync();

    Task<IEnumerable<AzureAppRegistrationWatch>> GetRegistrationsAsync();

    Task<AzureAppRegistrationWatch> RegisterApplicationAsync(RegisterAzureAppRegistrationRequest request);

    Task UnregisterApplicationAsync(int id);
}
