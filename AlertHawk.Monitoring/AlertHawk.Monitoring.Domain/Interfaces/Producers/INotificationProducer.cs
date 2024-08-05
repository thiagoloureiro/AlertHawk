using AlertHawk.Monitoring.Domain.Entities;

namespace AlertHawk.Monitoring.Domain.Interfaces.Producers;

public interface INotificationProducer
{
    Task HandleFailedNotifications(MonitorHttp monitorHttp, string? reasonPhrase);

    Task HandleSuccessNotifications(MonitorHttp monitorHttp, string? reasonPhrase);

    Task HandleSuccessTcpNotifications(MonitorTcp monitorTcp);

    Task HandleFailedTcpNotifications(MonitorTcp monitorTcp);
}