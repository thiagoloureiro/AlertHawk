namespace AlertHawk.Metrics;

public class PodMetricsItem
{
    public Metadata Metadata { get; set; }
    public ContainerMetrics[] Containers { get; set; }
}