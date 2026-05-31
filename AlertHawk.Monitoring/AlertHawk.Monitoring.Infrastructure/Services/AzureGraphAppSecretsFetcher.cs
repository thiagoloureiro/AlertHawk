using AlertHawk.Monitoring.Domain.Entities;
using AlertHawk.Monitoring.Domain.Interfaces.Services;
using Azure.Identity;
using Microsoft.Graph;
using System.Diagnostics.CodeAnalysis;

namespace AlertHawk.Monitoring.Infrastructure.Services;

[ExcludeFromCodeCoverage]
public class AzureGraphAppSecretsFetcher : IAzureAppSecretsFetcher
{
    private readonly IAzureSecretsSettingsProvider _settingsProvider;

    public AzureGraphAppSecretsFetcher(IAzureSecretsSettingsProvider settingsProvider)
    {
        _settingsProvider = settingsProvider;
    }

    public async IAsyncEnumerable<AzurePasswordCredentialInfo> FetchPasswordCredentialsAsync(
        IReadOnlyCollection<string> applicationObjectIds,
        [System.Runtime.CompilerServices.EnumeratorCancellation]
        CancellationToken cancellationToken = default)
    {
        if (applicationObjectIds.Count == 0)
        {
            yield break;
        }

        var graphClient = await CreateGraphClientAsync(cancellationToken);

        foreach (var objectId in applicationObjectIds.Distinct())
        {
            cancellationToken.ThrowIfCancellationRequested();

            Microsoft.Graph.Models.Application? application;
            try
            {
                application = await graphClient.Applications[objectId].GetAsync(requestConfiguration =>
                {
                    requestConfiguration.QueryParameters.Select =
                        ["id", "displayName", "appId", "passwordCredentials"];
                }, cancellationToken);
            }
            catch
            {
                continue;
            }

            if (application?.PasswordCredentials == null)
            {
                continue;
            }

            foreach (var password in application.PasswordCredentials)
            {
                if (password.KeyId == null || password.EndDateTime == null)
                {
                    continue;
                }

                yield return new AzurePasswordCredentialInfo(
                    application.Id ?? objectId,
                    application.DisplayName ?? "Unknown",
                    application.AppId ?? string.Empty,
                    password.KeyId.Value,
                    password.DisplayName,
                    password.EndDateTime.Value);
            }
        }
    }

    private async Task<GraphServiceClient> CreateGraphClientAsync(CancellationToken cancellationToken)
    {
        var options = await _settingsProvider.GetSettingsAsync();
        cancellationToken.ThrowIfCancellationRequested();

        var credential = new ClientSecretCredential(
            options.TenantId,
            options.ClientId,
            options.ClientSecret);

        return new GraphServiceClient(credential, ["https://graph.microsoft.com/.default"]);
    }
}
