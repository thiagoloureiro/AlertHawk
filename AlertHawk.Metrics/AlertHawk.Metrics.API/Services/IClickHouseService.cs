using AlertHawk.Metrics.API.Models;

namespace AlertHawk.Metrics.API.Services;

public interface IClickHouseService
{
    Task WriteMetricsAsync(
        string @namespace,
        string pod,
        string container,
        double cpuUsageCores,
        double? cpuLimitCores,
        double memoryUsageBytes,
        string? clusterName = null);

    Task WriteNodeMetricsAsync(
        string nodeName,
        double cpuUsageCores,
        double cpuCapacityCores,
        double memoryUsageBytes,
        double memoryCapacityBytes,
        string? clusterName = null);

    Task<List<PodMetricDto>> GetMetricsByNamespaceAsync(string? namespaceFilter = null, int? hours = 24, int limit = 100, string? clusterName = null);

    Task<List<NodeMetricDto>> GetNodeMetricsAsync(string? nodeNameFilter = null, int? hours = 24, int limit = 100, string? clusterName = null);

    Task<List<string>> GetUniqueClusterNamesAsync();

    Task CleanupMetricsAsync(int days);
}

