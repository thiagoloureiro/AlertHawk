using AlertHawk.Metrics.API.Entities;
using AlertHawk.Metrics.API.Repositories;
using System.Diagnostics.CodeAnalysis;

namespace AlertHawk.Metrics.API.Services;

[ExcludeFromCodeCoverage]
public class MetricsAlertService : IMetricsAlertService
{
    private readonly IMetricsAlertRepository _metricsAlertRepository;

    public MetricsAlertService(IMetricsAlertRepository metricsAlertRepository)
    {
        _metricsAlertRepository = metricsAlertRepository;
    }

    public async Task<IEnumerable<MetricsAlert>> GetMetricsAlerts(string? clusterName, string? nodeName, int? days)
    {
        return await _metricsAlertRepository.GetMetricsAlerts(clusterName, nodeName, days);
    }
}
