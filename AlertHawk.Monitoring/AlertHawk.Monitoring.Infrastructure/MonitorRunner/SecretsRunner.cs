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
        var options = await _settingsProvider.GetSettingsAsync();

        if (!options.Enabled)
        {
            return;
        }

        if (await _systemConfigurationRepository.IsMonitorExecutionDisabled())
        {
            _logger.LogInformation("Monitor execution is disabled. Skipping Azure secrets check.");
            return;
        }

        if (options.MonitorId <= 0)
        {
            _logger.LogWarning("AzureSecrets:MonitorId is not configured. Skipping Azure secrets check.");
            return;
        }

        if (string.IsNullOrWhiteSpace(options.TenantId) ||
            string.IsNullOrWhiteSpace(options.ClientId) ||
            string.IsNullOrWhiteSpace(options.ClientSecret))
        {
            _logger.LogWarning("Azure AD credentials are not fully configured. Skipping Azure secrets check.");
            return;
        }

        var registeredApps = (await _watchRepository.GetAllAsync()).ToList();
        if (registeredApps.Count == 0)
        {
            _logger.LogInformation("No app registrations registered for monitoring. Skipping Azure secrets check.");
            await _azureAppSecretRepository.DeleteExceptApplicationObjectIdsAsync(Array.Empty<string>());
            return;
        }

        var monitor = await _monitorRepository.GetMonitorById(options.MonitorId);
        if (monitor == null)
        {
            _logger.LogWarning("Monitor {MonitorId} not found for Azure secrets monitoring.", options.MonitorId);
            return;
        }

        var lastStatus = monitor.Status;
        var expiringSecrets = new List<AzureAppSecret>();
        var newlyExpiringSecrets = new List<AzureAppSecret>();
        var recoveredSecrets = new List<AzureAppSecret>();
        var now = DateTime.UtcNow;
        var registeredObjectIds = registeredApps.Select(a => a.ApplicationObjectId).ToList();

        try
        {
            var existingSecrets = (await _azureAppSecretRepository.GetAllAsync())
                .Where(s => registeredObjectIds.Contains(s.ApplicationObjectId))
                .ToDictionary(s => $"{s.ApplicationObjectId}:{s.KeyId}");

            await foreach (var credential in _azureAppSecretsFetcher.FetchPasswordCredentialsAsync(registeredObjectIds))
            {
                var daysUntilExpiry = (int)Math.Ceiling((credential.EndDateTime.UtcDateTime - now).TotalDays);
                var isExpiring = daysUntilExpiry <= options.DaysBeforeExpiryToAlert;
                var lookupKey = $"{credential.ApplicationObjectId}:{credential.KeyId}";
                var previouslyExpiring = existingSecrets.TryGetValue(lookupKey, out var existing) && existing.IsExpiring;

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

            await _azureAppSecretRepository.DeleteExceptApplicationObjectIdsAsync(registeredObjectIds);

            var succeeded = expiringSecrets.Count == 0;
            var responseMessage = SecretsRunnerMessages.BuildResponseMessage(expiringSecrets, succeeded);

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
                await _notificationProducer.HandleFailedSecretsNotifications(monitor, responseMessage);
                await _monitorAlertRepository.SaveMonitorAlert(monitorHistory, monitor.MonitorEnvironment);
            }
            else if (succeeded && !lastStatus)
            {
                await _notificationProducer.HandleSuccessSecretsNotifications(monitor,
                    "All Azure app registration secrets are within the expiry threshold.");
                await _monitorAlertRepository.SaveMonitorAlert(monitorHistory, monitor.MonitorEnvironment);
            }
            else if (newlyExpiringSecrets.Count > 0)
            {
                var alertMessage = SecretsRunnerMessages.BuildNewlyExpiringMessage(newlyExpiringSecrets);
                await _notificationProducer.HandleFailedSecretsNotifications(monitor, alertMessage);
            }
            else if (recoveredSecrets.Count > 0 && succeeded)
            {
                var recoveryMessage = SecretsRunnerMessages.BuildRecoveredMessage(recoveredSecrets);
                await _notificationProducer.HandleSuccessSecretsNotifications(monitor, recoveryMessage);
            }

            _logger.LogInformation(
                "Azure secrets check completed for {AppCount} registered app(s). Expiring: {ExpiringCount}, newly expiring: {NewCount}",
                registeredApps.Count, expiringSecrets.Count, newlyExpiringSecrets.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check Azure app registration secrets.");

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
