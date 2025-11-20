using System.Text.Json.Serialization;

namespace AlertHawk.Metrics;

public class NodeMetricsItem
{
    [JsonPropertyName("metadata")]
    public Metadata Metadata { get; set; } = new();

    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; }

    [JsonPropertyName("window")]
    public string? Window { get; set; }

    [JsonPropertyName("usage")]
    public ResourceUsage Usage { get; set; } = new();
}

