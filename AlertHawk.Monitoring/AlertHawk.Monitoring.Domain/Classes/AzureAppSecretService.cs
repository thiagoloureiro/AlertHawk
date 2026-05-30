using AlertHawk.Monitoring.Domain.Entities;
using AlertHawk.Monitoring.Domain.Interfaces.MonitorRunners;
using AlertHawk.Monitoring.Domain.Interfaces.Repositories;
using AlertHawk.Monitoring.Domain.Interfaces.Services;
using Monitor = AlertHawk.Monitoring.Domain.Entities.Monitor;

namespace AlertHawk.Monitoring.Domain.Classes;

public class AzureAppSecretService : IAzureAppSecretService
{
    private readonly IAzureAppSecretRepository _azureAppSecretRepository;
    private readonly IAzureSecretsSettingsProvider _settingsProvider;
    private readonly IMonitorService _monitorService;
    private readonly IMonitorHistoryService _monitorHistoryService;
    private readonly IMonitorAlertService _monitorAlertService;
    private readonly ISecretsRunner _secretsRunner;
    private readonly IMonitorGroupService _monitorGroupService;

    public AzureAppSecretService(
        IAzureAppSecretRepository azureAppSecretRepository,
        IAzureSecretsSettingsProvider settingsProvider,
        IMonitorService monitorService,
        IMonitorHistoryService monitorHistoryService,
        IMonitorAlertService monitorAlertService,
        ISecretsRunner secretsRunner,
        IMonitorGroupService monitorGroupService)
    {
        _azureAppSecretRepository = azureAppSecretRepository;
        _settingsProvider = settingsProvider;
        _monitorService = monitorService;
        _monitorHistoryService = monitorHistoryService;
        _monitorAlertService = monitorAlertService;
        _secretsRunner = secretsRunner;
        _monitorGroupService = monitorGroupService;
    }

    public async Task<IEnumerable<AzureAppSecret>> GetSecretsAsync(bool expiringOnly = false)
    {
        var secrets = await _azureAppSecretRepository.GetAllAsync();
        return expiringOnly ? secrets.Where(s => s.IsExpiring) : secrets;
    }

    public async Task<AzureSecretsStatusSummary> GetStatusAsync()
    {
        var settings = await _settingsProvider.GetSettingsAsync();
        var secrets = (await _azureAppSecretRepository.GetAllAsync()).ToList();

        Monitor? monitor = null;
        if (settings.MonitorId > 0)
        {
            try
            {
                monitor = await _monitorService.GetMonitorById(settings.MonitorId);
            }
            catch
            {
                monitor = null;
            }
        }

        return new AzureSecretsStatusSummary
        {
            Enabled = settings.Enabled,
            TotalSecrets = secrets.Count,
            ExpiringCount = secrets.Count(s => s.IsExpiring),
            LastChecked = secrets.Count > 0 ? secrets.Max(s => s.LastChecked) : null,
            MonitorStatus = monitor?.Status,
            MonitorId = settings.MonitorId,
            MonitorName = monitor?.Name,
            DaysBeforeExpiryToAlert = settings.DaysBeforeExpiryToAlert
        };
    }

    public async Task<IEnumerable<MonitorHistory>> GetHistoryAsync(int days)
    {
        var settings = await _settingsProvider.GetSettingsAsync();
        if (settings.MonitorId <= 0)
        {
            return Enumerable.Empty<MonitorHistory>();
        }

        return await _monitorHistoryService.GetMonitorHistory(settings.MonitorId, days, false, 0);
    }

    public async Task<IEnumerable<MonitorAlert>> GetAlertsAsync(int days, string? jwtToken)
    {
        var settings = await _settingsProvider.GetSettingsAsync();
        if (settings.MonitorId <= 0 || string.IsNullOrEmpty(jwtToken))
        {
            return Enumerable.Empty<MonitorAlert>();
        }

        return await _monitorAlertService.GetMonitorAlerts(settings.MonitorId, days, MonitorEnvironment.All, jwtToken);
    }

    public async Task<int> CreateAnchorMonitorAsync(AzureAppSecretMonitorRequest request, string? jwtToken)
    {
        var monitorHttp = new MonitorHttp
        {
            Name = request.Name.Trim(),
            MonitorTypeId = 1,
            MonitorGroup = request.MonitorGroup,
            MonitorRegion = (MonitorRegion)request.MonitorRegion,
            MonitorEnvironment = (MonitorEnvironment)request.MonitorEnvironment,
            HeartBeatInterval = request.HeartBeatInterval,
            Retries = 0,
            Status = true,
            Paused = true,
            UrlToCheck = "https://azure-app-registration.internal",
            CheckCertExpiry = false,
            IgnoreTlsSsl = true,
            Timeout = 5000,
            MaxRedirects = 0,
            MonitorHttpMethod = MonitorHttpMethod.Get,
            HttpResponseCodeFrom = 200,
            HttpResponseCodeTo = 299,
            CheckMonitorHttpHeaders = false
        };

        var monitorId = await _monitorService.CreateMonitorHttp(monitorHttp);
        await _monitorGroupService.AddMonitorToGroup(new MonitorGroupItems
        {
            MonitorId = monitorId,
            MonitorGroupId = request.MonitorGroup
        });
        await _settingsProvider.UpdateConfigAsync(new AzureSecretsConfigUpdateDto { MonitorId = monitorId });
        return monitorId;
    }

    public async Task UpdateAnchorMonitorAsync(AzureAppSecretMonitorUpdateRequest request, string? jwtToken)
    {
        var settings = await _settingsProvider.GetSettingsAsync();
        if (settings.MonitorId <= 0)
        {
            throw new InvalidOperationException("No anchor monitor is configured.");
        }

        var existing = await _monitorService.GetHttpMonitorByMonitorId(settings.MonitorId);
        existing.Name = request.Name.Trim();
        existing.MonitorGroup = request.MonitorGroup;
        existing.MonitorRegion = (MonitorRegion)request.MonitorRegion;
        existing.MonitorEnvironment = (MonitorEnvironment)request.MonitorEnvironment;
        existing.HeartBeatInterval = request.HeartBeatInterval;
        existing.Paused = true;

        await _monitorService.UpdateMonitorHttp(existing);
    }

    public async Task<Monitor?> GetAnchorMonitorAsync()
    {
        var settings = await _settingsProvider.GetSettingsAsync();
        if (settings.MonitorId <= 0)
        {
            return null;
        }

        try
        {
            return await _monitorService.GetMonitorById(settings.MonitorId);
        }
        catch
        {
            return null;
        }
    }

    public Task TriggerSyncAsync() => _secretsRunner.CheckSecretsAsync();
}
