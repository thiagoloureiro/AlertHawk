using AlertHawk.Monitoring.Domain.Entities.Report;

namespace AlertHawk.Monitoring.Domain.Interfaces.Repositories;

public interface IMonitorReportRepository
{
    Task<IEnumerable<MonitorReportUptime>> GetMonitorReportUptime(int groupId, int hours);
    Task<IEnumerable<MonitorReportAlerts>> GetMonitorAlerts(int groupId, int hours);
    Task<IEnumerable<MonitorReponseTime>> GetMonitorResponseTime(int groupId, int hours);
}