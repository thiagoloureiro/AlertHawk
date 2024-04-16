using AlertHawk.Monitoring.Domain.Entities.Report;

namespace AlertHawk.Monitoring.Domain.Interfaces.Services;

public interface IMonitorReportService
{
    Task<IEnumerable<MonitorReportUptime>> GetMonitorReportUptime(int groupId, int hours);
    Task<IEnumerable<MonitorReportAlerts>> GetMonitorAlerts(int groupId, int hours);
}