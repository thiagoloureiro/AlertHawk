namespace AlertHawk.Monitoring.Domain.Entities;

public class MonitorStatusDashboard
{
    public int MonitorUp { get; set; }
    public int MonitorDown { get; set; }
    public int MonitorPaused { get; set; }
}