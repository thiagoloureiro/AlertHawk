using AlertHawk.Monitoring.Domain.Entities;

namespace AlertHawk.Monitoring.Domain.Interfaces.Services;

public interface IAzureAppSecretService
{
    Task<IEnumerable<AzureAppSecret>> GetSecretsAsync(bool expiringOnly = false);

    Task<AzureSecretsStatusSummary> GetStatusAsync();

    Task<IEnumerable<MonitorHistory>> GetHistoryAsync(int days);

    Task<IEnumerable<MonitorAlert>> GetAlertsAsync(int days, string? jwtToken);

    Task<int> CreateAnchorMonitorAsync(AzureAppSecretMonitorRequest request, string? jwtToken);

    Task UpdateAnchorMonitorAsync(AzureAppSecretMonitorUpdateRequest request, string? jwtToken);

    Task<Entities.Monitor?> GetAnchorMonitorAsync();

    Task<IEnumerable<AzureAppRegistrationWatch>> GetRegistrationsAsync();

    Task<AzureAppRegistrationWatch> RegisterApplicationAsync(RegisterAzureAppRegistrationRequest request);

    Task UnregisterApplicationAsync(int id);
}
