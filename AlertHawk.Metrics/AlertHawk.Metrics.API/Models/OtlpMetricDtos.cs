namespace AlertHawk.Metrics.API.Models;

public class OtlpResource
{
    public Dictionary<string, string> Attributes { get; set; } = new();
    public string? HostName { get; set; }
    public string? Namespace { get; set; }
    public string? PodName { get; set; }
    public string? PodUid { get; set; }
    public string? ServiceName { get; set; }
    public string? ServiceVersion { get; set; }
}

public class OtlpScope
{
    public string? Name { get; set; }
    public string? Version { get; set; }
}

public enum OtlpMetricType
{
    Gauge,
    Sum,
    Histogram,
    Summary
}

public class OtlpDataPoint
{
    public Dictionary<string, string> Attributes { get; set; } = new();
    public DateTime Timestamp { get; set; }
    
    // For Gauge and Sum (NumberDataPoint)
    public double? Value { get; set; }
    public long? IntValue { get; set; }
    
    // For Histogram
    public ulong? Count { get; set; }
    public double? Sum { get; set; }
    public List<ulong>? BucketCounts { get; set; }
    public List<double>? ExplicitBounds { get; set; }
    
    // For Summary
    public List<OtlpQuantileValue>? QuantileValues { get; set; }
}

public class OtlpQuantileValue
{
    public double Quantile { get; set; }
    public double Value { get; set; }
}

public class OtlpMetric
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Unit { get; set; }
    public OtlpMetricType Type { get; set; }
    public bool? IsMonotonic { get; set; } // For Sum metrics
    public string? AggregationTemporality { get; set; } // For Sum and Histogram
    public List<OtlpDataPoint> DataPoints { get; set; } = new();
}

public class OtlpScopeMetrics
{
    public OtlpScope? Scope { get; set; }
    public List<OtlpMetric> Metrics { get; set; } = new();
}

public class OtlpResourceMetrics
{
    public OtlpResource Resource { get; set; } = new();
    public List<OtlpScopeMetrics> ScopeMetrics { get; set; } = new();
}

public class OtlpMetricsData
{
    public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;
    public List<OtlpResourceMetrics> ResourceMetrics { get; set; } = new();
}
