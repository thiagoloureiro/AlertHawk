namespace AlertHawk.Monitoring.Domain.Entities;

public class Monitor
{
    public int Id { get; set; }
    public MonitorType? MonitorType { get; set; }
    public int MonitorTypeId { get; set; }
    public required string Name { get; set; }
    public required int HeartBeatInterval { get; set; }
    public required int Retries { get; set; }
    public bool Status { get; set; }
    public int DaysToExpireCert { get; set; }
    public bool Paused { get; set; }
    public string? UrlToCheck { get; set; }
    public string? MonitorTcp { get; set; }
    public MonitorRegion MonitorRegion { get; set; }
    public MonitorEnvironment MonitorEnvironment { get; set; }
    public MonitorDashboard? MonitorStatusDashboard { get; set; }
    public string? Tag { get;set; }
    public bool CheckCertExpiry { get; set; }
}