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
        string? nodeName = null,
        string? podState = null,
        int restartCount = 0,
        long? podAge = null);

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
        bool? hasPidPressure = null,
        string? architecture = null,
        string? operatingSystem = null,
        string? region = null,
        string? instanceType = null);

    Task WritePodLogAsync(
        string @namespace,
        string pod,
        string container,
        string logContent);

    Task WriteKubernetesEventAsync(
        string @namespace,
        string eventName,
        string eventUid,
        string involvedObjectKind,
        string involvedObjectName,
        string involvedObjectNamespace,
        string eventType,
        string reason,
        string message,
        string sourceComponent,
        int count,
        DateTime? firstTimestamp,
        DateTime? lastTimestamp);

    Task WritePvcMetricAsync(
        string @namespace,
        string pod,
        string pvcNamespace,
        string pvcName,
        string? volumeName,
        ulong usedBytes,
        ulong availableBytes,
        ulong capacityBytes);

    /// <summary>
    /// Sends VM/host metrics (CPU, RAM, disks) to the API. Used when AGENT_TYPE=vm.
    /// </summary>
    Task WriteHostMetricAsync(
        string hostname,
        double cpuUsagePercent,
        ulong memoryTotalBytes,
        ulong memoryUsedBytes,
        IReadOnlyList<(string DriveName, ulong TotalBytes, ulong FreeBytes)>? disks = null);
}

