using AlertHawk.Monitoring.Domain.Entities;

namespace AlertHawk.Monitoring.Domain.Interfaces.Repositories;

public interface ISystemConfigurationRepository
{
    Task<SystemConfiguration?> GetSystemConfigurationByKey(string key);
    Task<bool> IsMonitorExecutionDisabled();
    Task UpsertSystemConfiguration(string key, string value, string? description = null);
    Task InitializeTableIfNotExists();
}
