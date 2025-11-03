using System.Text.Json.Serialization;

namespace AlertHawk.Metrics;

public class ContainerMetrics
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("usage")]
    public ResourceUsage Usage { get; set; } = new();
}

public class ResourceUsage
{
    [JsonPropertyName("cpu")]
    public string? Cpu { get; set; }

    [JsonPropertyName("memory")]
    public string? Memory { get; set; }
}