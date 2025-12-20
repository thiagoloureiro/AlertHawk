using AlertHawk.Metrics.API.Entities;
using AlertHawk.Metrics.API.Repositories;
using System.Diagnostics.CodeAnalysis;

namespace AlertHawk.Metrics.API.Services;

[ExcludeFromCodeCoverage]
public class MetricsNotificationService : IMetricsNotificationService
{
    private readonly IMetricsNotificationRepository _metricsNotificationRepository;

    public MetricsNotificationService(IMetricsNotificationRepository metricsNotificationRepository)
    {
        _metricsNotificationRepository = metricsNotificationRepository;
    }

    public async Task<IEnumerable<MetricsNotification>> GetMetricsNotifications(string clusterName)
    {
        return await _metricsNotificationRepository.GetMetricsNotifications(clusterName);
    }

    public async Task AddMetricsNotification(MetricsNotification metricsNotification)
    {
        await _metricsNotificationRepository.AddMetricsNotification(metricsNotification);
    }

    public async Task RemoveMetricsNotification(MetricsNotification metricsNotification)
    {
        await _metricsNotificationRepository.RemoveMetricsNotification(metricsNotification);
    }
}
