using System.Text.Json.Serialization;

namespace AlertHawk.Metrics;

public class PvcInfo
{
    public string Name { get; set; } = string.Empty;
    public string Namespace { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string StorageClassName { get; set; } = string.Empty;
    public double CapacityBytes { get; set; }
    public double? UsedBytes { get; set; }
    public string? VolumeName { get; set; }
}

