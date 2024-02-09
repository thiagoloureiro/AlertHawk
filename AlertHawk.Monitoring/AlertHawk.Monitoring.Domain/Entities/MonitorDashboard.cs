namespace AlertHawk.Monitoring.Domain.Entities;

public class MonitorDashboard
{
    public double Uptime24Hrs { get; set; }
    public double Uptime7Days { get; set; }
    public double Uptime30Days { get; set; }
    public double Uptime3Months { get; set; }
    public double Uptime6Months { get; set; }
    public double CertExpDays { get; set; }
    public double ResponseTime { get; set; }
}