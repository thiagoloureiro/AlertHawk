namespace AlertHawk.Monitoring.Domain.Entities;

public class MonitorGroup
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public IEnumerable<Monitor>? Monitors { get; set; }
}