using AlertHawk.Monitoring.Domain.Configuration;
using AlertHawk.Monitoring.Domain.Entities;
using AlertHawk.Monitoring.Domain.Interfaces.Repositories;
using AlertHawk.Monitoring.Domain.Interfaces.Services;
using Microsoft.Extensions.Options;
using System.Diagnostics.CodeAnalysis;

namespace AlertHawk.Monitoring.Infrastructure.Services;

[ExcludeFromCodeCoverage]
public class AzureSecretsSettingsProvider : IAzureSecretsSettingsProvider
{
    private const string KeyEnabled = "AzureSecrets:Enabled";
    private const string KeyDays = "AzureSecrets:DaysBeforeExpiryToAlert";
    private const string KeyMonitorId = "AzureSecrets:MonitorId";
    private const string KeyCron = "AzureSecrets:Cron";

    private readonly AzureSecretsOptions _defaults;
    private readonly ISystemConfigurationRepository _systemConfigurationRepository;

    public AzureSecretsSettingsProvider(
        IOptions<AzureSecretsOptions> defaults,
        ISystemConfigurationRepository systemConfigurationRepository)
    {
        _defaults = defaults.Value;
        _systemConfigurationRepository = systemConfigurationRepository;
    }

    public async Task<AzureSecretsOptions> GetSettingsAsync()
    {
        return new AzureSecretsOptions
        {
            Enabled = await GetBoolOverride(KeyEnabled, _defaults.Enabled),
            TenantId = _defaults.TenantId,
            ClientId = _defaults.ClientId,
            ClientSecret = _defaults.ClientSecret,
            DaysBeforeExpiryToAlert = await GetIntOverride(KeyDays, _defaults.DaysBeforeExpiryToAlert),
            MonitorId = await GetIntOverride(KeyMonitorId, _defaults.MonitorId),
            Cron = await GetStringOverride(KeyCron, _defaults.Cron)
        };
    }

    public async Task<AzureSecretsConfigDto> GetConfigDtoAsync()
    {
        var settings = await GetSettingsAsync();
        return new AzureSecretsConfigDto
        {
            Enabled = settings.Enabled,
            DaysBeforeExpiryToAlert = settings.DaysBeforeExpiryToAlert,
            MonitorId = settings.MonitorId,
            Cron = settings.Cron,
            HasCredentials = !string.IsNullOrWhiteSpace(settings.TenantId) &&
                             !string.IsNullOrWhiteSpace(settings.ClientId) &&
                             !string.IsNullOrWhiteSpace(settings.ClientSecret)
        };
    }

    public async Task UpdateConfigAsync(AzureSecretsConfigUpdateDto update)
    {
        if (update.Enabled.HasValue)
        {
            await _systemConfigurationRepository.UpsertSystemConfiguration(
                KeyEnabled,
                update.Enabled.Value.ToString().ToLower(),
                "Azure app registration secrets monitoring enabled");
        }

        if (update.DaysBeforeExpiryToAlert.HasValue)
        {
            await _systemConfigurationRepository.UpsertSystemConfiguration(
                KeyDays,
                update.DaysBeforeExpiryToAlert.Value.ToString(),
                "Days before Azure secret expiry to alert");
        }

        if (update.MonitorId.HasValue)
        {
            await _systemConfigurationRepository.UpsertSystemConfiguration(
                KeyMonitorId,
                update.MonitorId.Value.ToString(),
                "Monitor id used for Azure secrets notifications and history");
        }

        if (!string.IsNullOrWhiteSpace(update.Cron))
        {
            await _systemConfigurationRepository.UpsertSystemConfiguration(
                KeyCron,
                update.Cron,
                "Hangfire cron for Azure secrets sync");
        }
    }

    private async Task<bool> GetBoolOverride(string key, bool defaultValue)
    {
        var config = await _systemConfigurationRepository.GetSystemConfigurationByKey(key);
        return config != null && bool.TryParse(config.Value, out var value) ? value : defaultValue;
    }

    private async Task<int> GetIntOverride(string key, int defaultValue)
    {
        var config = await _systemConfigurationRepository.GetSystemConfigurationByKey(key);
        return config != null && int.TryParse(config.Value, out var value) ? value : defaultValue;
    }

    private async Task<string> GetStringOverride(string key, string defaultValue)
    {
        var config = await _systemConfigurationRepository.GetSystemConfigurationByKey(key);
        return config != null && !string.IsNullOrWhiteSpace(config.Value) ? config.Value : defaultValue;
    }
}
