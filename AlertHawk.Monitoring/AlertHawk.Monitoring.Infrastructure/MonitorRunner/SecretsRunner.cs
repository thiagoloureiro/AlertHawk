using AlertHawk.Monitoring.Domain.Classes;
using AlertHawk.Monitoring.Domain.Entities;
using AlertHawk.Monitoring.Domain.Interfaces.MonitorRunners;
using AlertHawk.Monitoring.Domain.Interfaces.Producers;
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
    private readonly IMonitorRepository _monitorRepository;
    private readonly INotificationProducer _notificationProducer;
    private readonly IMonitorAlertRepository _monitorAlertRepository;
    private readonly IMonitorHistoryRepository _monitorHistoryRepository;
    private readonly ISystemConfigurationRepository _systemConfigurationRepository;
    private readonly ILogger<SecretsRunner> _logger;

    public SecretsRunner(
        IAzureSecretsSettingsProvider settingsProvider,
        IAzureAppSecretsFetcher azureAppSecretsFetcher,
        IAzureAppSecretRepository azureAppSecretRepository,
        IAzureAppRegistrationWatchRepository watchRepository,
        IMonitorRepository monitorRepository,
        INotificationProducer notificationProducer,
        IMonitorAlertRepository monitorAlertRepository,
        IMonitorHistoryRepository monitorHistoryRepository,
        ISystemConfigurationRepository systemConfigurationRepository,
        ILogger<SecretsRunner> logger)
    {
        _settingsProvider = settingsProvider;
        _azureAppSecretsFetcher = azureAppSecretsFetcher;
        _azureAppSecretRepository = azureAppSecretRepository;
        _watchRepository = watchRepository;
        _monitorRepository = monitorRepository;
        _notificationProducer = notificationProducer;
        _monitorAlertRepository = monitorAlertRepository;
        _monitorHistoryRepository = monitorHistoryRepository;
        _systemConfigurationRepository = systemConfigurationRepository;
        _logger = logger;
    }

    public async Task CheckSecretsAsync()
    {
        var runId = Guid.NewGuid().ToString("N")[..8];
        _logger.LogInformation("[AzureSecrets][{RunId}] Hangfire job CheckAzureAppSecrets started at {UtcNow}", runId, DateTime.UtcNow);

        var options = await _settingsProvider.GetSettingsAsync();

        _logger.LogInformation(
            "[AzureSecrets][{RunId}] Resolved settings: Enabled={Enabled}, MonitorId={MonitorId}, DaysBeforeExpiryToAlert={Days}, Cron={Cron}, TenantId={TenantId}, ClientId={ClientId}, HasClientSecret={HasSecret}",
            runId,
            options.Enabled,
            options.MonitorId,
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

        if (options.MonitorId <= 0)
        {
            _logger.LogWarning("[AzureSecrets][{RunId}] Job exiting: MonitorId is not configured.", runId);
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

        var monitor = await _monitorRepository.GetMonitorById(options.MonitorId);
        if (monitor == null)
        {
            _logger.LogWarning("[AzureSecrets][{RunId}] Job exiting: anchor monitor {MonitorId} not found.", runId, options.MonitorId);
            return;
        }

        _logger.LogInformation(
            "[AzureSecrets][{RunId}] Anchor monitor loaded: Id={MonitorId}, Name={Name}, LastStatus={Status}",
            runId,
            monitor.Id,
            monitor.Name,
            monitor.Status);

        var lastStatus = monitor.Status;
        var expiringSecrets = new List<AzureAppSecret>();
        var newlyExpiringSecrets = new List<AzureAppSecret>();
        var recoveredSecrets = new List<AzureAppSecret>();
        var credentialsFetched = 0;
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
                var previouslyExpiring = existingSecrets.TryGetValue(lookupKey, out var existing) && existing.IsExpiring;

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
                    expiringSecrets.Add(secret);
                    if (!previouslyExpiring)
                    {
                        newlyExpiringSecrets.Add(secret);
                    }
                }
                else if (previouslyExpiring)
                {
                    recoveredSecrets.Add(secret);
                }
            }

            _logger.LogInformation(
                "[AzureSecrets][{RunId}] Graph returned {CredentialCount} credential(s). Upserted to database.",
                runId,
                credentialsFetched);

            await _azureAppSecretRepository.DeleteExceptApplicationObjectIdsAsync(registeredObjectIds);
            _logger.LogInformation("[AzureSecrets][{RunId}] Removed stale secrets for unregistered apps.", runId);

            var succeeded = expiringSecrets.Count == 0;
            var responseMessage = SecretsRunnerMessages.BuildResponseMessage(expiringSecrets, succeeded);

            _logger.LogInformation(
                "[AzureSecrets][{RunId}] Check result: succeeded={Succeeded}, expiring={Expiring}, newlyExpiring={New}, recovered={Recovered}",
                runId,
                succeeded,
                expiringSecrets.Count,
                newlyExpiringSecrets.Count,
                recoveredSecrets.Count);

            var monitorHistory = new MonitorHistory
            {
                MonitorId = monitor.Id,
                Status = succeeded,
                TimeStamp = now,
                ResponseMessage = responseMessage
            };

            await _monitorRepository.UpdateMonitorStatus(monitor.Id, succeeded, 0);
            await _monitorHistoryRepository.SaveMonitorHistory(monitorHistory);

            if (!succeeded && lastStatus)
            {
                _logger.LogInformation("[AzureSecrets][{RunId}] Sending failed notification (healthy → alert).", runId);
                await _notificationProducer.HandleFailedSecretsNotifications(monitor, responseMessage);
                await _monitorAlertRepository.SaveMonitorAlert(monitorHistory, monitor.MonitorEnvironment);
            }
            else if (succeeded && !lastStatus)
            {
                _logger.LogInformation("[AzureSecrets][{RunId}] Sending success notification (alert → healthy).", runId);
                await _notificationProducer.HandleSuccessSecretsNotifications(monitor,
                    "All Azure app registration secrets are within the expiry threshold.");
                await _monitorAlertRepository.SaveMonitorAlert(monitorHistory, monitor.MonitorEnvironment);
            }
            else if (newlyExpiringSecrets.Count > 0)
            {
                _logger.LogInformation("[AzureSecrets][{RunId}] Sending notification for newly expiring secret(s).", runId);
                var alertMessage = SecretsRunnerMessages.BuildNewlyExpiringMessage(newlyExpiringSecrets);
                await _notificationProducer.HandleFailedSecretsNotifications(monitor, alertMessage);
            }
            else if (recoveredSecrets.Count > 0 && succeeded)
            {
                _logger.LogInformation("[AzureSecrets][{RunId}] Sending recovery notification.", runId);
                var recoveryMessage = SecretsRunnerMessages.BuildRecoveredMessage(recoveredSecrets);
                await _notificationProducer.HandleSuccessSecretsNotifications(monitor, recoveryMessage);
            }

            _logger.LogInformation(
                "[AzureSecrets][{RunId}] Job completed successfully at {UtcNow}.",
                runId,
                DateTime.UtcNow);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[AzureSecrets][{RunId}] Job failed: {Message}", runId, ex.Message);

            var monitorHistory = new MonitorHistory
            {
                MonitorId = monitor.Id,
                Status = false,
                TimeStamp = now,
                ResponseMessage = $"Failed to check Azure secrets: {ex.Message}"
            };

            await _monitorHistoryRepository.SaveMonitorHistory(monitorHistory);
            await _monitorRepository.UpdateMonitorStatus(monitor.Id, false, 0);

            if (lastStatus)
            {
                await _notificationProducer.HandleFailedSecretsNotifications(monitor, monitorHistory.ResponseMessage);
                await _monitorAlertRepository.SaveMonitorAlert(monitorHistory, monitor.MonitorEnvironment);
            }

            SentrySdk.CaptureException(ex);
        }
    }
}
