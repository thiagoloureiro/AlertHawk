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
        double memoryUsageBytes);

    Task WriteNodeMetricAsync(
        string nodeName,
        double cpuUsageCores,
        double cpuCapacityCores,
        double memoryUsageBytes,
        double memoryCapacityBytes);
}

