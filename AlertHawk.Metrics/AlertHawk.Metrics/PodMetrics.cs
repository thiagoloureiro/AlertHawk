using System.Text.Json.Serialization;

namespace AlertHawk.Metrics;

public class PodMetricsList
{
    [JsonPropertyName("kind")]
    public string Kind { get; set; } = string.Empty;

    [JsonPropertyName("apiVersion")]
    public string ApiVersion { get; set; } = string.Empty;

    [JsonPropertyName("metadata")]
    public Metadata? Metadata { get; set; }

    [JsonPropertyName("items")]
    public PodMetricsItem[] Items { get; set; } = Array.Empty<PodMetricsItem>();
}