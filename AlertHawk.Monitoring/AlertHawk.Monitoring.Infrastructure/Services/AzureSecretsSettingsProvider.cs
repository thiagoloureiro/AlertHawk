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
            DaysBeforeExpiryToAlert = _defaults.DaysBeforeExpiryToAlert,
            Cron = _defaults.Cron
        };
    }

    public async Task<AzureSecretsConfigDto> GetConfigDtoAsync()
    {
        var settings = await GetSettingsAsync();
        return new AzureSecretsConfigDto
        {
            Enabled = settings.Enabled,
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
    }

    private async Task<bool> GetBoolOverride(string key, bool defaultValue)
    {
        var config = await _systemConfigurationRepository.GetSystemConfigurationByKey(key);
        return config != null && bool.TryParse(config.Value, out var value) ? value : defaultValue;
    }
}
