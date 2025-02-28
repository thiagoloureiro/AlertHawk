using System.Diagnostics.CodeAnalysis;

namespace AlertHawk.Monitoring.Domain.Entities;

[ExcludeFromCodeCoverage]
public class Monitor
{
    public int Id { get; set; }
    public MonitorType? MonitorType { get; set; }
    public int MonitorTypeId { get; set; }
    public string Name { get; set; }
    public int HeartBeatInterval { get; set; }
    public int Retries { get; set; }
    public bool Status { get; set; }
    public int DaysToExpireCert { get; set; }
    public bool Paused { get; set; }
    public string? UrlToCheck { get; set; }
    public MonitorRegion MonitorRegion { get; set; }
    public MonitorEnvironment MonitorEnvironment { get; set; }
    public MonitorDashboard? MonitorStatusDashboard { get; set; }
    public string? Tag { get; set; }
    public bool CheckCertExpiry { get; set; }
    public MonitorHttp? MonitorHttpItem { get; set; }
    public MonitorTcp? MonitorTcpItem { get; set; }
    public MonitorK8s? MonitorK8sItem { get; set; }
    public int MonitorGroup { get; set; }
}