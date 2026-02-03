using System.Text.Json.Serialization;

namespace AlertHawk.Metrics.Models;

/// <summary>
/// Mirrors kubelet /stats/summary API response for parsing PVC/volume usage.
/// </summary>
public class StatsSummary
{
    [JsonPropertyName("node")]
    public NodeStatsSummary? Node { get; set; }

    [JsonPropertyName("pods")]
    public List<PodStatsSummary> Pods { get; set; } = [];
}

public class NodeStatsSummary
{
    [JsonPropertyName("nodeName")]
    public string? NodeName { get; set; }
}

/// <summary>
/// Pod reference and volume stats from kubelet summary.
/// </summary>
public class PodStatsSummary
{
    [JsonPropertyName("podRef")]
    public PodRefSummary? PodRef { get; set; }

    [JsonPropertyName("volume")]
    public List<VolumeStatsSummary> Volume { get; set; } = [];
}

public class PodRefSummary
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("namespace")]
    public string? Namespace { get; set; }

    [JsonPropertyName("uid")]
    public string? Uid { get; set; }
}

/// <summary>
/// Volume stats (usedBytes, availableBytes, capacityBytes); may include pvcRef.
/// </summary>
public class VolumeStatsSummary
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("usedBytes")]
    public ulong? UsedBytes { get; set; }

    [JsonPropertyName("availableBytes")]
    public ulong? AvailableBytes { get; set; }

    [JsonPropertyName("capacityBytes")]
    public ulong? CapacityBytes { get; set; }

    [JsonPropertyName("pvcRef")]
    public PvcRefSummary? PvcRef { get; set; }
}

public class PvcRefSummary
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("namespace")]
    public string? Namespace { get; set; }
}
