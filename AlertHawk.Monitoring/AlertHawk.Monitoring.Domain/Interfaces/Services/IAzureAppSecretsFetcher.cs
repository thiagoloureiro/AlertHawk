using AlertHawk.Monitoring.Domain.Entities;

namespace AlertHawk.Monitoring.Domain.Interfaces.Services;

public interface IAzureAppSecretsFetcher
{
    Task<IEnumerable<AzureAppRegistrationSummary>> DiscoverApplicationsAsync(
        CancellationToken cancellationToken = default);

    IAsyncEnumerable<AzurePasswordCredentialInfo> FetchPasswordCredentialsAsync(
        IReadOnlyCollection<string> applicationObjectIds,
        CancellationToken cancellationToken = default);
}
