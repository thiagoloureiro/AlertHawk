using AlertHawk.Monitoring.Domain.Entities;
using AlertHawk.Monitoring.Domain.Interfaces.Repositories;
using AlertHawk.Monitoring.Domain.Interfaces.Services;

namespace AlertHawk.Monitoring.Domain.Classes;

public class AzureAppSecretService : IAzureAppSecretService
{
    private readonly IAzureAppSecretRepository _azureAppSecretRepository;
    private readonly IAzureSecretsSettingsProvider _settingsProvider;
    private readonly IAzureAppRegistrationWatchRepository _watchRepository;

    public AzureAppSecretService(
        IAzureAppSecretRepository azureAppSecretRepository,
        IAzureSecretsSettingsProvider settingsProvider,
        IAzureAppRegistrationWatchRepository watchRepository)
    {
        _azureAppSecretRepository = azureAppSecretRepository;
        _settingsProvider = settingsProvider;
        _watchRepository = watchRepository;
    }

    public async Task<IEnumerable<AzureAppSecret>> GetSecretsAsync(bool expiringOnly = false)
    {
        var registeredIds = (await _watchRepository.GetAllAsync())
            .Select(w => w.ApplicationObjectId)
            .ToHashSet();

        var secrets = (await _azureAppSecretRepository.GetAllAsync())
            .Where(s => registeredIds.Contains(s.ApplicationObjectId));

        return expiringOnly ? secrets.Where(s => s.IsExpiring) : secrets;
    }

    public async Task<AzureSecretsStatusSummary> GetStatusAsync()
    {
        var settings = await _settingsProvider.GetSettingsAsync();
        var registeredApps = (await _watchRepository.GetAllAsync()).ToList();
        var registeredIds = registeredApps.Select(w => w.ApplicationObjectId).ToHashSet();
        var secrets = (await _azureAppSecretRepository.GetAllAsync())
            .Where(s => registeredIds.Contains(s.ApplicationObjectId))
            .ToList();

        return new AzureSecretsStatusSummary
        {
            Enabled = settings.Enabled,
            TotalSecrets = secrets.Count,
            ExpiringCount = secrets.Count(s => s.IsExpiring),
            LastChecked = secrets.Count > 0 ? secrets.Max(s => s.LastChecked) : null,
            RegisteredAppsCount = registeredApps.Count
        };
    }

    public Task<IEnumerable<AzureAppRegistrationWatch>> GetRegistrationsAsync() =>
        _watchRepository.GetAllAsync(enabledOnly: false);

    public async Task<AzureAppRegistrationWatch> RegisterApplicationAsync(RegisterAzureAppRegistrationRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.ApplicationObjectId))
        {
            throw new ArgumentException("ApplicationObjectId is required.");
        }

        var existing = await _watchRepository.GetByObjectIdAsync(request.ApplicationObjectId);
        if (existing != null)
        {
            return existing;
        }

        var watch = new AzureAppRegistrationWatch
        {
            ApplicationObjectId = request.ApplicationObjectId.Trim(),
            ApplicationDisplayName = request.ApplicationDisplayName?.Trim() ?? "Unknown",
            AppId = request.AppId?.Trim() ?? string.Empty,
            IsEnabled = true,
            CreatedAt = DateTime.UtcNow
        };

        watch.Id = await _watchRepository.AddAsync(watch);
        return watch;
    }

    public async Task UnregisterApplicationAsync(int id)
    {
        var registrations = (await _watchRepository.GetAllAsync(enabledOnly: false)).ToList();
        var registration = registrations.FirstOrDefault(r => r.Id == id)
            ?? throw new InvalidOperationException("Registration not found.");

        await _watchRepository.DeleteAsync(id);
        await _azureAppSecretRepository.DeleteByApplicationObjectIdAsync(registration.ApplicationObjectId);
    }
}
