using System.Text.Json.Serialization;

namespace AlertHawk.Metrics;

public class Metadata
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("namespace")]
    public string Namespace { get; set; } = string.Empty;

    [JsonPropertyName("creationTimestamp")]
    public DateTime? CreationTimestamp { get; set; }

    [JsonPropertyName("selfLink")]
    public string? SelfLink { get; set; }

    [JsonPropertyName("labels")]
    public Dictionary<string, string>? Labels { get; set; }
}

