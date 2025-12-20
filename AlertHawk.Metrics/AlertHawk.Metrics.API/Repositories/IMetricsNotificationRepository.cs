using AlertHawk.Metrics.API.Entities;

namespace AlertHawk.Metrics.API.Repositories;

public interface IMetricsNotificationRepository
{
    Task AddMetricsNotification(MetricsNotification metricsNotification);

    Task RemoveMetricsNotification(MetricsNotification metricsNotification);

    Task<IEnumerable<MetricsNotification>> GetMetricsNotifications(string clusterName);
}
