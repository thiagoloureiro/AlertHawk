using AlertHawk.Metrics.API.Entities;

namespace AlertHawk.Metrics.API.Services;

public interface IMetricsNotificationService
{
    Task<IEnumerable<MetricsNotification>> GetMetricsNotifications(string clusterName);

    Task AddMetricsNotification(MetricsNotification metricsNotification);

    Task RemoveMetricsNotification(MetricsNotification metricsNotification);
}
