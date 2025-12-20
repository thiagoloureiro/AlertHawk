using AlertHawk.Metrics.API.Entities;

namespace AlertHawk.Metrics.API.Services;

public interface IMetricsAlertService
{
    Task<IEnumerable<MetricsAlert>> GetMetricsAlerts(string? clusterName, string? nodeName, int? days);
}
