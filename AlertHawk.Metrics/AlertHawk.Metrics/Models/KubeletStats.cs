using System.Text.Json.Serialization;

namespace AlertHawk.Metrics;

public class KubeletStatsSummary
{
    [JsonPropertyName("node")]
    public NodeStats? Node { get; set; }

    [JsonPropertyName("pods")]
    public PodStats[] Pods { get; set; } = Array.Empty<PodStats>();
}

public class NodeStats
{
    [JsonPropertyName("fs")]
    public FsStats? Fs { get; set; }
}

public class PodStats
{
    [JsonPropertyName("podRef")]
    public PodRef? PodRef { get; set; }

    [JsonPropertyName("containers")]
    public ContainerStats[] Containers { get; set; } = Array.Empty<ContainerStats>();
}

public class PodRef
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("namespace")]
    public string Namespace { get; set; } = string.Empty;
}

public class ContainerStats
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("rootfs")]
    public FsStats? Rootfs { get; set; }

    [JsonPropertyName("logs")]
    public FsStats? Logs { get; set; }
}

public class FsStats
{
    [JsonPropertyName("ioStats")]
    public IoStats? IoStats { get; set; }
}

public class IoStats
{
    [JsonPropertyName("readBytes")]
    public ulong? ReadBytes { get; set; }

    [JsonPropertyName("writeBytes")]
    public ulong? WriteBytes { get; set; }

    [JsonPropertyName("readOps")]
    public ulong? ReadOps { get; set; }

    [JsonPropertyName("writeOps")]
    public ulong? WriteOps { get; set; }
}
