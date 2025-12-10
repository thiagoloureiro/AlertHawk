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

