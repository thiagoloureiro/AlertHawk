namespace AlertHawk.Monitoring.Domain.Entities;

public class MonitorAgent
{
    public int Id { get; set; }
    public required string Hostname { get; set; }
    public required DateTime TimeStamp { get; set; }
    public bool IsMaster { get; set; }
    public int ListTasks { get; set; }
}