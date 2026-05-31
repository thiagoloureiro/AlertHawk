using AlertHawk.Monitoring.Domain.Entities;

namespace AlertHawk.Monitoring.Domain.Interfaces.Services;

public interface IAzureAppSecretsFetcher
{
    IAsyncEnumerable<AzurePasswordCredentialInfo> FetchPasswordCredentialsAsync(
        IReadOnlyCollection<string> applicationObjectIds,
        CancellationToken cancellationToken = default);
}
