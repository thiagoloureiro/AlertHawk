namespace AlertHawk.Monitoring.Domain.Entities;

public class MonitorHistory
{
    public long Id { get; set; }
    public int MonitorId { get; set; }
    public bool Status { get; set; }
    public DateTime TimeStamp { get; set; }
    public int StatusCode { get; set; }
    public int ResponseTime { get; set; }
    public string HttpVersion { get; set; }
}