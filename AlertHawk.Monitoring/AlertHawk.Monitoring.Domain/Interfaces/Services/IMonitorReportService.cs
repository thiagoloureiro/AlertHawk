using AlertHawk.Monitoring.Domain.Entities.Report;

namespace AlertHawk.Monitoring.Domain.Interfaces.Services;

public interface IMonitorReportService
{
    Task<IEnumerable<MonitorReportUptime>> GetMonitorReportUptime(int groupId, int hours, string filter);

    Task<IEnumerable<MonitorReportAlerts>> GetMonitorAlerts(int groupId, int hours);

    Task<IEnumerable<MonitorReponseTime>> GetMonitorResponseTime(int groupId, int hours, string filter);

    Task<IEnumerable<MonitorReportUptime>> GetMonitorReportUptime(int groupId, DateTime startDate, DateTime endDate);
}