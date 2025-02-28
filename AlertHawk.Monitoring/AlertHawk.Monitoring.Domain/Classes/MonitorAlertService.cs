using AlertHawk.Monitoring.Domain.Entities;
using AlertHawk.Monitoring.Domain.Interfaces.Repositories;
using AlertHawk.Monitoring.Domain.Interfaces.Services;

namespace AlertHawk.Monitoring.Domain.Classes;

public class MonitorAlertService : IMonitorAlertService
{
    private readonly IMonitorAlertRepository _monitorAlertRepository;
    private readonly IMonitorGroupService _monitorGroupService;

    public MonitorAlertService(IMonitorAlertRepository monitorAlertRepository, IMonitorGroupService monitorGroupService)
    {
        _monitorAlertRepository = monitorAlertRepository;
        _monitorGroupService = monitorGroupService;
    }

    public async Task<IEnumerable<MonitorAlert>> GetMonitorAlerts(int? monitorId, int? days,
        MonitorEnvironment? environment, string jwtToken)
    {
        var groupIds = await _monitorGroupService.GetUserGroupMonitorListIds(jwtToken);

        if (groupIds != null && groupIds.Any())
        {
            var alerts = await _monitorAlertRepository.GetMonitorAlerts(monitorId, days, environment, groupIds);

            // Group alerts by MonitorId
            var groupedAlerts = alerts.GroupBy(a => a.MonitorId);

            foreach (var monitorAlerts in groupedAlerts)
            {
                // Order alerts chronologically for each monitor
                var orderedAlerts = monitorAlerts.OrderBy(a => a.TimeStamp).ToList();

                for (int i = 0; i < orderedAlerts.Count; i++)
                {
                    if (orderedAlerts[i].Status == false) // Went offline
                    {
                        // Find the next occurrence of status 1 (monitor came back online) for the same MonitorId
                        var nextOnlineAlert = orderedAlerts.Skip(i + 1).FirstOrDefault(a => a.Status);

                        if (nextOnlineAlert != null)
                        {
                            // Calculate the offline period in minutes
                            orderedAlerts[i].PeriodOffline =
                                Math.Max(1, (int)(nextOnlineAlert.TimeStamp - orderedAlerts[i].TimeStamp).TotalMinutes);
                        }
                    }
                }
            }

            return groupedAlerts.SelectMany(g => g).ToList().Where(x => x.Status == false);
        }

        return new List<MonitorAlert>();
    }

    public async Task<MemoryStream> GetMonitorAlertsReport(int? monitorId, int? days, string jwtToken,
        MonitorEnvironment? environment,
        ReportType reportType)
    {
        var monitorAlerts = await GetMonitorAlerts(monitorId, days, environment, jwtToken);

        return reportType switch
        {
            ReportType.Excel => await _monitorAlertRepository.CreateExcelFileAsync(monitorAlerts),
            _ => new MemoryStream()
        };
    }

    public async Task<IEnumerable<MonitorAlert>> GetMonitorAlertsByMonitorGroup(int monitorGroupId, int? days, MonitorEnvironment? environment, string jwtToken)
    {
        var monitorList = await _monitorGroupService.GetMonitorListByGroupId(monitorGroupId);
        var listIds = monitorList.Select(x => x.Id).ToList();
        
        return await _monitorAlertRepository.GetMonitorAlertsByMonitorGroup(listIds, days, environment);
    }
}