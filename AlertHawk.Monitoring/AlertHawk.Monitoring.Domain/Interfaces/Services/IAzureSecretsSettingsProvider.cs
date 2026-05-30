using AlertHawk.Monitoring.Domain.Configuration;
using AlertHawk.Monitoring.Domain.Entities;

namespace AlertHawk.Monitoring.Domain.Interfaces.Services;

public interface IAzureSecretsSettingsProvider
{
    Task<AzureSecretsOptions> GetSettingsAsync();

    Task<AzureSecretsConfigDto> GetConfigDtoAsync();

    Task UpdateConfigAsync(AzureSecretsConfigUpdateDto update);
}
