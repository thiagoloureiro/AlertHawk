namespace AlertHawk.Metrics;

public class PodLog
{
    public string Namespace { get; set; } = string.Empty;
    public string Pod { get; set; } = string.Empty;
    public string Container { get; set; } = string.Empty;
    public string LogContent { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}

