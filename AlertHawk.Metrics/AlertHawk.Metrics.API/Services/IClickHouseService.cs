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
        string? clusterName = null,
        string? nodeName = null);

    Task WriteNodeMetricsAsync(
        string nodeName,
        double cpuUsageCores,
        double cpuCapacityCores,
        double memoryUsageBytes,
        double memoryCapacityBytes,
        string? clusterName = null,
        string? clusterEnvironment = null,
        string? kubernetesVersion = null,
        string? cloudProvider = null,
        bool? isReady = null,
        bool? hasMemoryPressure = null,
        bool? hasDiskPressure = null,
        bool? hasPidPressure = null,
        string? architecture = null,
        string? operatingSystem = null,
        string? region = null,
        string? instanceType = null);

    Task<List<PodMetricDto>> GetMetricsByNamespaceAsync(string? namespaceFilter = null, int? minutes = 1440, string? clusterName = null);

    Task<List<NodeMetricDto>> GetNodeMetricsAsync(string? nodeNameFilter = null, int? minutes = 1440, string? clusterName = null);

    Task<List<string>> GetUniqueClusterNamesAsync();

    Task<List<string>> GetUniqueNamespaceNamesAsync(string? clusterName = null);

    Task CleanupMetricsAsync(int days);

    Task WritePodLogAsync(
        string @namespace,
        string pod,
        string container,
        string logContent,
        string? clusterName = null);

    Task<List<PodLogDto>> GetPodLogsAsync(
        string? namespaceFilter = null,
        string? podFilter = null,
        string? containerFilter = null,
        int? minutes = 1440,
        int limit = 100,
        string? clusterName = null);

    Task CleanupSystemLogsAsync();

    Task<List<TableSizeDto>> GetTableSizesAsync();
}

