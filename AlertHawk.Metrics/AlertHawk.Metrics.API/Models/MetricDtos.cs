namespace AlertHawk.Metrics.API.Models;

public class PodMetricDto
{
    public DateTime Timestamp { get; set; }
    public string ClusterName { get; set; } = string.Empty;
    public string Namespace { get; set; } = string.Empty;
    public string Pod { get; set; } = string.Empty;
    public string Container { get; set; } = string.Empty;
    public double CpuUsageCores { get; set; }
    public double? CpuLimitCores { get; set; }
    public double MemoryUsageBytes { get; set; }
    public string? NodeName { get; set; }
    public string? PodState { get; set; }
    public int RestartCount { get; set; }
    public long? PodAge { get; set; }
}

public class NodeMetricDto
{
    public DateTime Timestamp { get; set; }
    public string ClusterName { get; set; } = string.Empty;
    public string? ClusterEnvironment { get; set; }
    public string NodeName { get; set; } = string.Empty;
    public double CpuUsageCores { get; set; }
    public double CpuCapacityCores { get; set; }
    public double MemoryUsageBytes { get; set; }
    public double MemoryCapacityBytes { get; set; }
    public string? KubernetesVersion { get; set; }
    public string? CloudProvider { get; set; }
    public bool? IsReady { get; set; }
    public bool? HasMemoryPressure { get; set; }
    public bool? HasDiskPressure { get; set; }
    public bool? HasPidPressure { get; set; }
    public string? Architecture { get; set; }
    public string? OperatingSystem { get; set; }
    public string? Region { get; set; }
    public string? InstanceType { get; set; }
}

public class PodMetricRequest
{
    public string ClusterName { get; set; } = string.Empty;
    public string Namespace { get; set; } = string.Empty;
    public string Pod { get; set; } = string.Empty;
    public string Container { get; set; } = string.Empty;
    public double CpuUsageCores { get; set; }
    public double? CpuLimitCores { get; set; }
    public double MemoryUsageBytes { get; set; }
    public string? NodeName { get; set; }
    public string? PodState { get; set; }
    public int RestartCount { get; set; }
    public long? PodAge { get; set; }
}

public class NodeMetricRequest
{
    public string ClusterName { get; set; } = string.Empty;
    public string? ClusterEnvironment { get; set; }
    public string NodeName { get; set; } = string.Empty;
    public double CpuUsageCores { get; set; }
    public double CpuCapacityCores { get; set; }
    public double MemoryUsageBytes { get; set; }
    public double MemoryCapacityBytes { get; set; }
    public string? KubernetesVersion { get; set; }
    public string? CloudProvider { get; set; }
    public bool? IsReady { get; set; }
    public bool? HasMemoryPressure { get; set; }
    public bool? HasDiskPressure { get; set; }
    public bool? HasPidPressure { get; set; }
    public string? Architecture { get; set; }
    public string? OperatingSystem { get; set; }
    public string? Region { get; set; }
    public string? InstanceType { get; set; }
}

public class PodLogDto
{
    public DateTime Timestamp { get; set; }
    public string ClusterName { get; set; } = string.Empty;
    public string Namespace { get; set; } = string.Empty;
    public string Pod { get; set; } = string.Empty;
    public string Container { get; set; } = string.Empty;
    public string LogContent { get; set; } = string.Empty;
}

public class PodLogRequest
{
    public string ClusterName { get; set; } = string.Empty;
    public string Namespace { get; set; } = string.Empty;
    public string Pod { get; set; } = string.Empty;
    public string Container { get; set; } = string.Empty;
    public string LogContent { get; set; } = string.Empty;
}

public class TableSizeDto
{
    public string Database { get; set; } = string.Empty;
    public string Table { get; set; } = string.Empty;
    public string TotalSize { get; set; } = string.Empty;
    public long TotalSizeBytes { get; set; }
}

public class KubernetesEventDto
{
    public DateTime Timestamp { get; set; }
    public string ClusterName { get; set; } = string.Empty;
    public string Namespace { get; set; } = string.Empty;
    public string EventName { get; set; } = string.Empty;
    public string EventUid { get; set; } = string.Empty;
    public string InvolvedObjectKind { get; set; } = string.Empty;
    public string InvolvedObjectName { get; set; } = string.Empty;
    public string InvolvedObjectNamespace { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string SourceComponent { get; set; } = string.Empty;
    public int Count { get; set; }
    public DateTime? FirstTimestamp { get; set; }
    public DateTime? LastTimestamp { get; set; }
}

public class KubernetesEventRequest
{
    public string ClusterName { get; set; } = string.Empty;
    public string Namespace { get; set; } = string.Empty;
    public string EventName { get; set; } = string.Empty;
    public string EventUid { get; set; } = string.Empty;
    public string InvolvedObjectKind { get; set; } = string.Empty;
    public string InvolvedObjectName { get; set; } = string.Empty;
    public string InvolvedObjectNamespace { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string SourceComponent { get; set; } = string.Empty;
    public int Count { get; set; }
    public DateTime? FirstTimestamp { get; set; }
    public DateTime? LastTimestamp { get; set; }
}

public class PvcMetricDto
{
    public DateTime Timestamp { get; set; }
    public string ClusterName { get; set; } = string.Empty;
    public string Namespace { get; set; } = string.Empty;
    public string Pod { get; set; } = string.Empty;
    public string PvcNamespace { get; set; } = string.Empty;
    public string PvcName { get; set; } = string.Empty;
    public string? VolumeName { get; set; }
    public ulong UsedBytes { get; set; }
    public ulong AvailableBytes { get; set; }
    public ulong CapacityBytes { get; set; }
}

public class PvcMetricRequest
{
    public string ClusterName { get; set; } = string.Empty;
    public string Namespace { get; set; } = string.Empty;
    public string Pod { get; set; } = string.Empty;
    public string PvcNamespace { get; set; } = string.Empty;
    public string PvcName { get; set; } = string.Empty;
    public string? VolumeName { get; set; }
    public ulong UsedBytes { get; set; }
    public ulong AvailableBytes { get; set; }
    public ulong CapacityBytes { get; set; }
}

public class ClusterPriceDto
{
    public DateTime Timestamp { get; set; }
    public string ClusterName { get; set; } = string.Empty;
    public string NodeName { get; set; } = string.Empty;
    public string? Region { get; set; }
    public string? InstanceType { get; set; }
    public string? OperatingSystem { get; set; }
    public string? CloudProvider { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
    public double UnitPrice { get; set; }
    public double RetailPrice { get; set; }
    public string MeterName { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string SkuName { get; set; } = string.Empty;
    public string ServiceName { get; set; } = string.Empty;
    public string ArmRegionName { get; set; } = string.Empty;
    public DateTime EffectiveStartDate { get; set; }
}

/// <summary>
/// VM/Host metrics: CPU, RAM, and optional disk list. Sent by the VM agent with hostname.
/// </summary>
public class HostMetricRequest
{
    public string Hostname { get; set; } = string.Empty;
    public double CpuUsagePercent { get; set; }
    public ulong MemoryTotalBytes { get; set; }
    public ulong MemoryUsedBytes { get; set; }
    public List<HostDiskMetricItem>? Disks { get; set; }
}

public class HostDiskMetricItem
{
    public string DriveName { get; set; } = string.Empty;
    public ulong TotalBytes { get; set; }
    public ulong FreeBytes { get; set; }
}

public class HostMetricDto
{
    public DateTime Timestamp { get; set; }
    public string Hostname { get; set; } = string.Empty;
    public double CpuUsagePercent { get; set; }
    public ulong MemoryTotalBytes { get; set; }
    public ulong MemoryUsedBytes { get; set; }
}

public class HostDiskMetricDto
{
    public DateTime Timestamp { get; set; }
    public string Hostname { get; set; } = string.Empty;
    public string DriveName { get; set; } = string.Empty;
    public ulong TotalBytes { get; set; }
    public ulong FreeBytes { get; set; }
}
