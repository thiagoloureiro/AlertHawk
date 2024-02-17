namespace AlertHawk.Monitoring.Domain.Entities;

public class MonitorAlert
{
    public int Id { get; set; }
    public int MonitorId { get; set; }
    public DateTime TimeStamp { get; set; }
    public bool Status { get; set; }
    public string? Message { get; set; }
}