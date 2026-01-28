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

    [JsonPropertyName("storage")]
    public string? Storage { get; set; }

    [JsonPropertyName("ephemeral-storage")]
    public string? EphemeralStorage { get; set; }

    [JsonPropertyName("network")]
    public string? Network { get; set; }
}

