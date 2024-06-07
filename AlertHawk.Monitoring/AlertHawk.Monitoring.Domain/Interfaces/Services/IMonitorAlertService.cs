using AlertHawk.Monitoring.Domain.Entities;

namespace AlertHawk.Monitoring.Domain.Interfaces.Services;

public interface IMonitorAlertService
{
    Task<IEnumerable<MonitorAlert>> GetMonitorAlerts(int? monitorId, int? days, MonitorEnvironment? environment,
        string jwtToken);
    Task<MemoryStream> GetMonitorAlertsReport(int? monitorId, int? days, string jwtToken, ReportType reportType);
}