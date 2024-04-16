namespace AlertHawk.Monitoring.Domain.Entities.Report;

public class MonitorReponseTime
{
    public string MonitorName { get; set; }
    public double AvgResponseTime { get; set; }
    public double MaxResponseTime { get; set; }
    public double MinResponseTime { get; set; }
}