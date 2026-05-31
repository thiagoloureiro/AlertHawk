using AlertHawk.Monitoring.Domain.Entities;
using AlertHawk.Monitoring.Domain.Interfaces.Services;
using Azure.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using System.Diagnostics.CodeAnalysis;

namespace AlertHawk.Monitoring.Infrastructure.Services;

[ExcludeFromCodeCoverage]
public class AzureGraphAppSecretsFetcher : IAzureAppSecretsFetcher
{
    private readonly IAzureSecretsSettingsProvider _settingsProvider;
    private readonly ILogger<AzureGraphAppSecretsFetcher> _logger;

    public AzureGraphAppSecretsFetcher(
        IAzureSecretsSettingsProvider settingsProvider,
        ILogger<AzureGraphAppSecretsFetcher> logger)
    {
        _settingsProvider = settingsProvider;
        _logger = logger;
    }

    public async IAsyncEnumerable<AzurePasswordCredentialInfo> FetchPasswordCredentialsAsync(
        IReadOnlyCollection<string> applicationObjectIds,
        [System.Runtime.CompilerServices.EnumeratorCancellation]
        CancellationToken cancellationToken = default)
    {
        if (applicationObjectIds.Count == 0)
        {
            _logger.LogWarning("[AzureSecrets] Graph fetch skipped: no application object IDs provided.");
            yield break;
        }

        _logger.LogInformation(
            "[AzureSecrets] Starting Graph fetch for {AppCount} application(s): {ObjectIds}",
            applicationObjectIds.Count,
            string.Join(", ", applicationObjectIds.Distinct()));

        var graphClient = await CreateGraphClientAsync(cancellationToken);

        foreach (var objectId in applicationObjectIds.Distinct())
        {
            cancellationToken.ThrowIfCancellationRequested();

            _logger.LogInformation(
                "[AzureSecrets] Calling Microsoft Graph GET /applications/{ObjectId}?$select=id,displayName,appId,passwordCredentials",
                objectId);

            Microsoft.Graph.Models.Application? application;
            try
            {
                application = await graphClient.Applications[objectId].GetAsync(requestConfiguration =>
                {
                    requestConfiguration.QueryParameters.Select =
                        ["id", "displayName", "appId", "passwordCredentials"];
                }, cancellationToken);

                _logger.LogInformation(
                    "[AzureSecrets] Graph response for {ObjectId}: displayName={DisplayName}, appId={AppId}, passwordCredentialCount={Count}",
                    objectId,
                    application?.DisplayName ?? "(null)",
                    application?.AppId ?? "(null)",
                    application?.PasswordCredentials?.Count ?? 0);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "[AzureSecrets] Graph call failed for application ObjectId={ObjectId}. Message={Message}",
                    objectId,
                    ex.Message);
                continue;
            }

            if (application?.PasswordCredentials == null || application.PasswordCredentials.Count == 0)
            {
                _logger.LogWarning(
                    "[AzureSecrets] No password credentials returned for {DisplayName} ({ObjectId}).",
                    application?.DisplayName ?? objectId,
                    objectId);
                continue;
            }

            var yielded = 0;
            foreach (var password in application.PasswordCredentials)
            {
                if (password.KeyId == null || password.EndDateTime == null)
                {
                    _logger.LogDebug(
                        "[AzureSecrets] Skipping credential without KeyId or EndDateTime on app {ObjectId}.",
                        objectId);
                    continue;
                }

                _logger.LogDebug(
                    "[AzureSecrets] Credential found: app={DisplayName}, keyId={KeyId}, name={SecretName}, expires={EndDate}",
                    application.DisplayName,
                    password.KeyId,
                    password.DisplayName ?? "(unnamed)",
                    password.EndDateTime);

                yield return new AzurePasswordCredentialInfo(
                    application.Id ?? objectId,
                    application.DisplayName ?? "Unknown",
                    application.AppId ?? string.Empty,
                    password.KeyId.Value,
                    password.DisplayName,
                    password.EndDateTime.Value);

                yielded++;
            }

            _logger.LogInformation(
                "[AzureSecrets] Yielded {Count} secret(s) for app {DisplayName} ({ObjectId}).",
                yielded,
                application.DisplayName,
                objectId);
        }

        _logger.LogInformation("[AzureSecrets] Graph fetch completed for all registered applications.");
    }

    private async Task<GraphServiceClient> CreateGraphClientAsync(CancellationToken cancellationToken)
    {
        var options = await _settingsProvider.GetSettingsAsync();
        cancellationToken.ThrowIfCancellationRequested();

        _logger.LogInformation(
            "[AzureSecrets] Creating Graph client. TenantId={TenantId}, ClientId={ClientId}, HasClientSecret={HasSecret}",
            options.TenantId,
            options.ClientId,
            !string.IsNullOrWhiteSpace(options.ClientSecret));

        try
        {
            var credential = new ClientSecretCredential(
                options.TenantId,
                options.ClientId,
                options.ClientSecret);

            var client = new GraphServiceClient(credential, ["https://graph.microsoft.com/.default"]);
            _logger.LogInformation("[AzureSecrets] Graph client created successfully.");
            return client;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[AzureSecrets] Failed to create Graph client / obtain token.");
            throw;
        }
    }
}
