using System.Text.Json.Serialization;

namespace AlertHawk.Metrics;

public class PodMetricsItem
{
    [JsonPropertyName("metadata")]
    public Metadata Metadata { get; set; } = new();

    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; }

    [JsonPropertyName("window")]
    public string Window { get; set; } = string.Empty;

    [JsonPropertyName("containers")]
    public ContainerMetrics[] Containers { get; set; } = Array.Empty<ContainerMetrics>();
}

