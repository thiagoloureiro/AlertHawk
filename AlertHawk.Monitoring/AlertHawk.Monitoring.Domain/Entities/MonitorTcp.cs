namespace AlertHawk.Monitoring.Domain.Entities;

public class MonitorTcp : Monitor
{
    public int MonitorId { get; set; }
    public required int Port { get; set; }
    public required string IP { get; set; }
    public required int Timeout { get; set; }
    public bool LastStatus { get; set; }
    public string Response { get; set; }
}