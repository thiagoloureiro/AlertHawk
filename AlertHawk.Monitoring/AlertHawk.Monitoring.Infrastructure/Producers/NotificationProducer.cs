using AlertHawk.Monitoring.Domain.Entities;
using AlertHawk.Monitoring.Domain.Interfaces.Producers;
using AlertHawk.Monitoring.Domain.Interfaces.Repositories;
using MassTransit;
using SharedModels;
using System.Diagnostics.CodeAnalysis;

namespace AlertHawk.Monitoring.Infrastructure.Producers;

[ExcludeFromCodeCoverage]
public class NotificationProducer : INotificationProducer
{
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly IMonitorNotificationRepository _monitorNotificationRepository;

    public NotificationProducer(IPublishEndpoint publishEndpoint, IMonitorNotificationRepository monitorNotificationRepository)
    {
        _publishEndpoint = publishEndpoint;
        _monitorNotificationRepository = monitorNotificationRepository;
    }

    public async Task HandleFailedNotifications(MonitorHttp monitorHttp, string? reasonPhrase)
    {
        var notificationIdList = await _monitorNotificationRepository.GetMonitorNotifications(monitorHttp.MonitorId);

        Console.WriteLine(
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

        Console.WriteLine(
            $"sending success notification calling {monitorHttp.UrlToCheck}, Response StatusCode: {monitorHttp.ResponseStatusCode}");

        foreach (var item in notificationIdList)
        {
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

        Console.WriteLine(
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

        Console.WriteLine(
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