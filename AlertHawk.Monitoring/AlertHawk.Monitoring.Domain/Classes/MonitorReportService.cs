using AlertHawk.Monitoring.Domain.Entities.Report;
using AlertHawk.Monitoring.Domain.Interfaces.Repositories;
using AlertHawk.Monitoring.Domain.Interfaces.Services;

namespace AlertHawk.Monitoring.Domain.Classes;

public class MonitorReportService: IMonitorReportService
{
    private IMonitorReportRepository _monitorReportRepository;

    public MonitorReportService(IMonitorReportRepository monitorReportRepository)
    {
        _monitorReportRepository = monitorReportRepository;
    }

    public async Task<IEnumerable<MonitorReportUptime>>GetMonitorReportUptime(int groupId, int hours)
    {
        return await _monitorReportRepository.GetMonitorReportUptime(groupId, hours);
    }

    public async Task<IEnumerable<MonitorReportAlerts>> GetMonitorAlerts(int groupId, int hours)
    {
        return await _monitorReportRepository.GetMonitorAlerts(groupId, hours);
    }

    public async Task<IEnumerable<MonitorReponseTime>> GetMonitorResponseTime(int groupId, int hours)
    {
        return await _monitorReportRepository.GetMonitorResponseTime(groupId, hours);
    }
}