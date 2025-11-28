namespace AlertHawk.Metrics;

/// <summary>
/// Interface for MetricsApiClient to enable testing.
/// </summary>
public interface IMetricsApiClient
{
    Task WritePodMetricAsync(
        string @namespace,
        string pod,
        string container,
        double cpuUsageCores,
        double? cpuLimitCores,
        double memoryUsageBytes,
        string? nodeName = null);

    Task WriteNodeMetricAsync(
        string nodeName,
        double cpuUsageCores,
        double cpuCapacityCores,
        double memoryUsageBytes,
        double memoryCapacityBytes,
        string? kubernetesVersion = null,
        string? cloudProvider = null,
        bool? isReady = null,
        bool? hasMemoryPressure = null,
        bool? hasDiskPressure = null,
        bool? hasPidPressure = null);
}

