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

    public async Task<IEnumerable<AzureAppRegistrationSummary>> DiscoverApplicationsAsync(
        CancellationToken cancellationToken = default)
    {
        var graphClient = await CreateGraphClientAsync(cancellationToken);
        var results = new List<AzureAppRegistrationSummary>();

        var response = await graphClient.Applications.GetAsync(requestConfiguration =>
        {
            requestConfiguration.QueryParameters.Select = ["id", "displayName", "appId"];
            requestConfiguration.QueryParameters.Top = 999;
        }, cancellationToken);

        while (response?.Value != null)
        {
            cancellationToken.ThrowIfCancellationRequested();

            foreach (var application in response.Value)
            {
                if (string.IsNullOrEmpty(application.Id))
                {
                    continue;
                }

                results.Add(new AzureAppRegistrationSummary
                {
                    ApplicationObjectId = application.Id,
                    ApplicationDisplayName = application.DisplayName ?? "Unknown",
                    AppId = application.AppId ?? string.Empty
                });
            }

            if (string.IsNullOrEmpty(response.OdataNextLink))
            {
                break;
            }

            response = await graphClient.Applications
                .WithUrl(response.OdataNextLink)
                .GetAsync(cancellationToken: cancellationToken);
        }

        return results.OrderBy(a => a.ApplicationDisplayName);
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
