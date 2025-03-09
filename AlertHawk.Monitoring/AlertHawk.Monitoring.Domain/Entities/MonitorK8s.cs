namespace AlertHawk.Monitoring.Domain.Entities;

public class MonitorK8s: Monitor
{
    public int MonitorId { get; set; }
    public string ClusterName { get; set; }
    public string? KubeConfig { get; set; }
    public bool LastStatus { get; set; }
}