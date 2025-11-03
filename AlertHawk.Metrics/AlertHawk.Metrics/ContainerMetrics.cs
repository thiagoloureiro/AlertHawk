namespace AlertHawk.Metrics;

public class ContainerMetrics
{
    public string Name { get; set; }
    public IDictionary<string, string> Usage { get; set; }
}