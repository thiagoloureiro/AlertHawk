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
        [System.Runtime.CompilerServices.EnumeratorCancellation]
        CancellationToken cancellationToken = default)
    {
        var options = await _settingsProvider.GetSettingsAsync();

        var credential = new ClientSecretCredential(
            options.TenantId,
            options.ClientId,
            options.ClientSecret);

        var graphClient = new GraphServiceClient(credential, ["https://graph.microsoft.com/.default"]);

        var response = await graphClient.Applications.GetAsync(requestConfiguration =>
        {
            requestConfiguration.QueryParameters.Select =
                ["id", "displayName", "appId", "passwordCredentials"];
            requestConfiguration.QueryParameters.Top = 999;
        }, cancellationToken);

        while (response?.Value != null)
        {
            cancellationToken.ThrowIfCancellationRequested();

            foreach (var application in response.Value)
            {
                if (application.PasswordCredentials == null)
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
                        application.Id ?? string.Empty,
                        application.DisplayName ?? "Unknown",
                        application.AppId ?? string.Empty,
                        password.KeyId.Value,
                        password.DisplayName,
                        password.EndDateTime.Value);
                }
            }

            if (string.IsNullOrEmpty(response.OdataNextLink))
            {
                yield break;
            }

            response = await graphClient.Applications
                .WithUrl(response.OdataNextLink)
                .GetAsync(cancellationToken: cancellationToken);
        }
    }
}
