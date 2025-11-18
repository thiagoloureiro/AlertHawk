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
}

public class NodeMetricDto
{
    public DateTime Timestamp { get; set; }
    public string ClusterName { get; set; } = string.Empty;
    public string NodeName { get; set; } = string.Empty;
    public double CpuUsageCores { get; set; }
    public double CpuCapacityCores { get; set; }
    public double MemoryUsageBytes { get; set; }
    public double MemoryCapacityBytes { get; set; }
}

public class PvcMetricDto
{
    public DateTime Timestamp { get; set; }
    public string ClusterName { get; set; } = string.Empty;
    public string Namespace { get; set; } = string.Empty;
    public string PvcName { get; set; } = string.Empty;
    public string StorageClass { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public double CapacityBytes { get; set; }
    public double? UsedBytes { get; set; }
    public string? VolumeName { get; set; }
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
}

public class NodeMetricRequest
{
    public string ClusterName { get; set; } = string.Empty;
    public string NodeName { get; set; } = string.Empty;
    public double CpuUsageCores { get; set; }
    public double CpuCapacityCores { get; set; }
    public double MemoryUsageBytes { get; set; }
    public double MemoryCapacityBytes { get; set; }
}

public class PvcMetricRequest
{
    public string ClusterName { get; set; } = string.Empty;
    public string Namespace { get; set; } = string.Empty;
    public string PvcName { get; set; } = string.Empty;
    public string StorageClass { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public double CapacityBytes { get; set; }
    public double? UsedBytes { get; set; }
    public string? VolumeName { get; set; }
}

