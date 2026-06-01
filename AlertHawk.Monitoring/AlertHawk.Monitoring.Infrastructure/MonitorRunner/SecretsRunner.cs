using AlertHawk.Monitoring.Domain.Entities;
using AlertHawk.Monitoring.Domain.Interfaces.MonitorRunners;
using AlertHawk.Monitoring.Domain.Interfaces.Repositories;
using AlertHawk.Monitoring.Domain.Interfaces.Services;
using Microsoft.Extensions.Logging;

namespace AlertHawk.Monitoring.Infrastructure.MonitorRunner;

public class SecretsRunner : ISecretsRunner
{
    private readonly IAzureSecretsSettingsProvider _settingsProvider;
    private readonly IAzureAppSecretsFetcher _azureAppSecretsFetcher;
    private readonly IAzureAppSecretRepository _azureAppSecretRepository;
    private readonly IAzureAppRegistrationWatchRepository _watchRepository;
    private readonly ISystemConfigurationRepository _systemConfigurationRepository;
    private readonly ILogger<SecretsRunner> _logger;

    public SecretsRunner(
        IAzureSecretsSettingsProvider settingsProvider,
        IAzureAppSecretsFetcher azureAppSecretsFetcher,
        IAzureAppSecretRepository azureAppSecretRepository,
        IAzureAppRegistrationWatchRepository watchRepository,
        ISystemConfigurationRepository systemConfigurationRepository,
        ILogger<SecretsRunner> logger)
    {
        _settingsProvider = settingsProvider;
        _azureAppSecretsFetcher = azureAppSecretsFetcher;
        _azureAppSecretRepository = azureAppSecretRepository;
        _watchRepository = watchRepository;
        _systemConfigurationRepository = systemConfigurationRepository;
        _logger = logger;
    }

    public async Task CheckSecretsAsync()
    {
        var runId = Guid.NewGuid().ToString("N")[..8];
        _logger.LogInformation("[AzureSecrets][{RunId}] Hangfire job CheckAzureAppSecrets started at {UtcNow}", runId, DateTime.UtcNow);

        var options = await _settingsProvider.GetSettingsAsync();

        _logger.LogInformation(
            "[AzureSecrets][{RunId}] Resolved settings: Enabled={Enabled}, DaysBeforeExpiryToAlert={Days}, Cron={Cron}, TenantId={TenantId}, ClientId={ClientId}, HasClientSecret={HasSecret}",
            runId,
            options.Enabled,
            options.DaysBeforeExpiryToAlert,
            options.Cron,
            options.TenantId,
            options.ClientId,
            !string.IsNullOrWhiteSpace(options.ClientSecret));

        if (!options.Enabled)
        {
            _logger.LogInformation("[AzureSecrets][{RunId}] Job exiting: monitoring is disabled.", runId);
            return;
        }

        if (await _systemConfigurationRepository.IsMonitorExecutionDisabled())
        {
            _logger.LogInformation("[AzureSecrets][{RunId}] Job exiting: monitor execution is disabled (maintenance).", runId);
            return;
        }

        if (string.IsNullOrWhiteSpace(options.TenantId) ||
            string.IsNullOrWhiteSpace(options.ClientId) ||
            string.IsNullOrWhiteSpace(options.ClientSecret))
        {
            _logger.LogWarning(
                "[AzureSecrets][{RunId}] Job exiting: Azure AD credentials incomplete (TenantId={HasTenant}, ClientId={HasClient}, ClientSecret={HasSecret}).",
                runId,
                !string.IsNullOrWhiteSpace(options.TenantId),
                !string.IsNullOrWhiteSpace(options.ClientId),
                !string.IsNullOrWhiteSpace(options.ClientSecret));
            return;
        }

        var registeredApps = (await _watchRepository.GetAllAsync()).ToList();
        if (registeredApps.Count == 0)
        {
            _logger.LogInformation("[AzureSecrets][{RunId}] Job exiting: no app registrations in watchlist.", runId);
            await _azureAppSecretRepository.DeleteExceptApplicationObjectIdsAsync(Array.Empty<string>());
            return;
        }

        foreach (var app in registeredApps)
        {
            _logger.LogInformation(
                "[AzureSecrets][{RunId}] Registered app: {DisplayName} | ObjectId={ObjectId} | AppId={AppId}",
                runId,
                app.ApplicationDisplayName,
                app.ApplicationObjectId,
                app.AppId);
        }

        var credentialsFetched = 0;
        var expiringCount = 0;
        var now = DateTime.UtcNow;
        var registeredObjectIds = registeredApps.Select(a => a.ApplicationObjectId).ToList();

        try
        {
            var existingSecrets = (await _azureAppSecretRepository.GetAllAsync())
                .Where(s => registeredObjectIds.Contains(s.ApplicationObjectId))
                .ToDictionary(s => $"{s.ApplicationObjectId}:{s.KeyId}");

            _logger.LogInformation(
                "[AzureSecrets][{RunId}] Existing secrets in DB for registered apps: {Count}",
                runId,
                existingSecrets.Count);

            _logger.LogInformation("[AzureSecrets][{RunId}] Calling Azure Graph for registered applications...", runId);

            await foreach (var credential in _azureAppSecretsFetcher.FetchPasswordCredentialsAsync(registeredObjectIds))
            {
                credentialsFetched++;
                var daysUntilExpiry = (int)Math.Ceiling((credential.EndDateTime.UtcDateTime - now).TotalDays);
                var isExpiring = daysUntilExpiry <= options.DaysBeforeExpiryToAlert;
                var lookupKey = $"{credential.ApplicationObjectId}:{credential.KeyId}";

                _logger.LogInformation(
                    "[AzureSecrets][{RunId}] Processing secret: {App} | keyId={KeyId} | daysUntilExpiry={Days} | isExpiring={IsExpiring}",
                    runId,
                    credential.ApplicationDisplayName,
                    credential.KeyId,
                    daysUntilExpiry,
                    isExpiring);

                var secret = new AzureAppSecret
                {
                    ApplicationObjectId = credential.ApplicationObjectId,
                    ApplicationDisplayName = credential.ApplicationDisplayName,
                    AppId = credential.AppId,
                    KeyId = credential.KeyId,
                    SecretDisplayName = credential.SecretDisplayName,
                    EndDateTime = credential.EndDateTime,
                    DaysUntilExpiry = daysUntilExpiry,
                    IsExpiring = isExpiring,
                    LastChecked = now
                };

                await _azureAppSecretRepository.UpsertAsync(secret);
                existingSecrets.Remove(lookupKey);

                if (isExpiring)
                {
                    expiringCount++;
                }
            }

            _logger.LogInformation(
                "[AzureSecrets][{RunId}] Graph returned {CredentialCount} credential(s). Upserted to database. Expiring={Expiring}",
                runId,
                credentialsFetched,
                expiringCount);

            await _azureAppSecretRepository.DeleteExceptApplicationObjectIdsAsync(registeredObjectIds);
            _logger.LogInformation("[AzureSecrets][{RunId}] Removed stale secrets for unregistered apps.", runId);

            _logger.LogInformation(
                "[AzureSecrets][{RunId}] Job completed successfully at {UtcNow}.",
                runId,
                DateTime.UtcNow);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[AzureSecrets][{RunId}] Job failed: {Message}", runId, ex.Message);
            SentrySdk.CaptureException(ex);
        }
    }
}
