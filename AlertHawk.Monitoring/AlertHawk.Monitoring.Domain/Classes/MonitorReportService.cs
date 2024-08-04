using AlertHawk.Monitoring.Domain.Entities.Report;
using AlertHawk.Monitoring.Domain.Interfaces.Repositories;
using AlertHawk.Monitoring.Domain.Interfaces.Services;

namespace AlertHawk.Monitoring.Domain.Classes;

public class MonitorReportService : IMonitorReportService
{
    private IMonitorReportRepository _monitorReportRepository;

    public MonitorReportService(IMonitorReportRepository monitorReportRepository)
    {
        _monitorReportRepository = monitorReportRepository;
    }

    public async Task<IEnumerable<MonitorReportUptime>> GetMonitorReportUptime(int groupId, int hours, string filter)
    {
        var result = await _monitorReportRepository.GetMonitorReportUptime(groupId, hours);

        var monitorReportUptimes = result.ToList();
        foreach (var report in monitorReportUptimes)
        {
            double uptimePercentage = (double)report.TotalOnlineMinutes /
                (report.TotalOnlineMinutes + report.TotalOfflineMinutes) * 100;
            report.UptimePercentage = uptimePercentage;
        }

        if (!string.IsNullOrWhiteSpace(filter))
        {
            monitorReportUptimes = monitorReportUptimes.Where(x => x.MonitorName != null && x.MonitorName.Contains(filter, StringComparison.CurrentCultureIgnoreCase)).ToList();
        }

        return monitorReportUptimes;
    }

    public async Task<IEnumerable<MonitorReportAlerts>> GetMonitorAlerts(int groupId, int hours)
    {
        return await _monitorReportRepository.GetMonitorAlerts(groupId, hours);
    }

    public async Task<IEnumerable<MonitorReponseTime>> GetMonitorResponseTime(int groupId, int hours, string? filter)
    {
        var result = await _monitorReportRepository.GetMonitorResponseTime(groupId, hours);

        if (!string.IsNullOrWhiteSpace(filter))
        {
            result = result.Where(x => x.MonitorName != null && x.MonitorName.Contains(filter, StringComparison.CurrentCultureIgnoreCase)).ToList();
        }

        return result;
    }

    public async Task<IEnumerable<MonitorReportUptime>> GetMonitorReportUptime(int groupId, DateTime startDate,
        DateTime endDate)
    {
        var result = await _monitorReportRepository.GetMonitorReportUptime(groupId, startDate, endDate);

        var monitorReportUptimes = result.ToList();
        foreach (var report in monitorReportUptimes)
        {
            double uptimePercentage = (double)report.TotalOnlineMinutes /
                (report.TotalOnlineMinutes + report.TotalOfflineMinutes) * 100;
            report.UptimePercentage = uptimePercentage;
        }

        return monitorReportUptimes;
    }
}