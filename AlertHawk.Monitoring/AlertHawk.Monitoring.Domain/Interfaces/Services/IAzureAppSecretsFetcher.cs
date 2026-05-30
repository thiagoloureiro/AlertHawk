using AlertHawk.Monitoring.Domain.Entities;

namespace AlertHawk.Monitoring.Domain.Interfaces.Services;

public interface IAzureAppSecretsFetcher
{
    IAsyncEnumerable<AzurePasswordCredentialInfo> FetchPasswordCredentialsAsync(
        CancellationToken cancellationToken = default);
}
