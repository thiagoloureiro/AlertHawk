using AlertHawk.Monitoring.Domain.Entities;
using AlertHawk.Monitoring.Domain.Interfaces.Producers;
using AlertHawk.Monitoring.Domain.Interfaces.Repositories;
using MassTransit;
using SharedModels;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;

namespace AlertHawk.Monitoring.Infrastructure.Producers;

[ExcludeFromCodeCoverage]
public class NotificationProducer : INotificationProducer
{
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly IMonitorNotificationRepository _monitorNotificationRepository;
    private readonly ILogger<NotificationProducer> _notificationLogger;

    public NotificationProducer(IPublishEndpoint publishEndpoint,
        IMonitorNotificationRepository monitorNotificationRepository, ILogger<NotificationProducer> notificationLogger)
    {
        _publishEndpoint = publishEndpoint;
        _monitorNotificationRepository = monitorNotificationRepository;
        _notificationLogger = notificationLogger;
    }

    public async Task HandleFailedNotifications(MonitorHttp monitorHttp, string? reasonPhrase)
    {
        var notificationIdList = await _monitorNotificationRepository.GetMonitorNotifications(monitorHttp.MonitorId);

        _notificationLogger.LogInformation(
            $"sending notification Error calling {monitorHttp.UrlToCheck}, Response StatusCode: {monitorHttp.ResponseStatusCode}");

        foreach (var item in notificationIdList)
        {
            await _publishEndpoint.Publish<NotificationAlert>(new
            {
                NotificationId = item.NotificationId,
                TimeStamp = DateTime.UtcNow,
                Message =
                    $"Error calling {monitorHttp.Name}, Response StatusCode: {monitorHttp.ResponseStatusCode}",
                ReasonPhrase = reasonPhrase,
                StatusCode = (int)monitorHttp.ResponseStatusCode
            });
        }
    }

    public async Task HandleSuccessNotifications(MonitorHttp monitorHttp, string? reasonPhrase)
    {
        var notificationIdList = await _monitorNotificationRepository.GetMonitorNotifications(monitorHttp.MonitorId);

        _notificationLogger.LogInformation(
            $"sending success notification calling {monitorHttp.UrlToCheck}, Response StatusCode: {monitorHttp.ResponseStatusCode}");

        foreach (var item in notificationIdList)
        {
            _notificationLogger.LogInformation(
                $"Notification Details: notificationId {item.NotificationId}, monitorId: {item.MonitorId}, ResponseStatusCode: {monitorHttp.ResponseStatusCode}, reasonPhrase {reasonPhrase}, name:  {monitorHttp.Name}");
            await _publishEndpoint.Publish<NotificationAlert>(new
            {
                NotificationId = item.NotificationId,
                TimeStamp = DateTime.UtcNow,
                Message =
                    $"Success calling {monitorHttp.Name}, Response StatusCode: {monitorHttp.ResponseStatusCode}",
                StatusCode = (int)monitorHttp.ResponseStatusCode,
                ReasonPhrase = reasonPhrase
            });
        }
    }

    public async Task HandleSuccessTcpNotifications(MonitorTcp monitorTcp)
    {
        var notificationIdList = await _monitorNotificationRepository.GetMonitorNotifications(monitorTcp.MonitorId);

        _notificationLogger.LogInformation(
            $"sending success notification calling {monitorTcp.IP} Port: {monitorTcp.Port},");

        foreach (var item in notificationIdList)
        {
            await _publishEndpoint.Publish<NotificationAlert>(new
            {
                NotificationId = item.NotificationId,
                TimeStamp = DateTime.UtcNow,
                Message =
                    $"Success calling {monitorTcp.Name}, Response StatusCode: {monitorTcp.Response}"
            });
        }
    }

    public async Task HandleFailedTcpNotifications(MonitorTcp monitorTcp)
    {
        var notificationIdList = await _monitorNotificationRepository.GetMonitorNotifications(monitorTcp.MonitorId);

        _notificationLogger.LogInformation(
            $"sending notification Error calling {monitorTcp.IP} Port: {monitorTcp.Port}, Response: {monitorTcp.Response}");

        foreach (var item in notificationIdList)
        {
            await _publishEndpoint.Publish<NotificationAlert>(new
            {
                NotificationId = item.NotificationId,
                TimeStamp = DateTime.UtcNow,
                Message =
                    $"Error calling {monitorTcp.Name}, Response StatusCode: {monitorTcp.Response}"
            });
        }
    }
}