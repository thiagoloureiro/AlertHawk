using AlertHawk.Metrics.API.Entities;

namespace AlertHawk.Metrics.API.Repositories;

public interface IMetricsAlertRepository
{
    Task<IEnumerable<MetricsAlert>> GetMetricsAlerts(string? clusterName, string? nodeName, int? days);

    Task SaveMetricsAlert(MetricsAlert metricsAlert);
}
