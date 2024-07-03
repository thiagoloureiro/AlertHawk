using System.Diagnostics.CodeAnalysis;

namespace AlertHawk.Monitoring.Domain.Entities;

public class MonitorBackup: Monitor
{
    public IEnumerable<MonitorHttp> MonitorHttpList { get; set; }
    public IEnumerable<MonitorTcp> MonitorTcpList { get; set; }
}