using AlertHawk.Monitoring.Domain.Entities;
using AlertHawk.Monitoring.Domain.Interfaces.Producers;
using AlertHawk.Monitoring.Domain.Interfaces.Repositories;
using MassTransit;
using Microsoft.Extensions.Logging;
using SharedModels;
using System.Diagnostics.CodeAnalysis;

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

        try
        {
            foreach (var item in notificationIdList)
            {
                _notificationLogger.LogInformation(
                    $"Notification Details: notificationId {item.NotificationId}, monitorId: {item.MonitorId}, ResponseStatusCode: {monitorHttp.ResponseStatusCode}, reasonPhrase {reasonPhrase}, name:  {monitorHttp.Name}");
                await _publishEndpoint.Publish<NotificationAlert>(new
                {
                    NotificationId = item.NotificationId,
                    MonitorId = item.MonitorId,
                    Service = monitorHttp.Name,
                    Region = (int)monitorHttp?.MonitorRegion,
                    Environment = (int)monitorHttp?.MonitorEnvironment,
                    URL = monitorHttp?.UrlToCheck,
                    Success = false,
                    TimeStamp = DateTime.UtcNow,
                    Message = $"Error calling {monitorHttp?.Name}, Response StatusCode: {monitorHttp?.ResponseStatusCode}",
                    StatusCode = (int)monitorHttp.ResponseStatusCode,
                    ReasonPhrase = reasonPhrase,
                });

                _notificationLogger.LogInformation("Notification sent successfully");
            }
        }
        catch (Exception err)
        {
            _notificationLogger.LogError($"Error in HandleFailedNotifications: {err.Message}");
            Sentry.SentrySdk.CaptureException(err);
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
                MonitorId = item.MonitorId,
                Service = monitorHttp.Name,
                Region = (int)monitorHttp?.MonitorRegion,
                Environment = (int)monitorHttp?.MonitorEnvironment,
                URL = monitorHttp?.UrlToCheck,
                Success = true,
                TimeStamp = DateTime.UtcNow,
                Message = $"Success calling {monitorHttp?.Name}, Response StatusCode: {monitorHttp?.ResponseStatusCode}",
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
                MonitorId = item.MonitorId,
                Service = monitorTcp.Name,
                Region = (int)monitorTcp.MonitorRegion,
                Environment = (int)monitorTcp.MonitorEnvironment,
                IP = monitorTcp.IP,
                Port = monitorTcp.Port,
                Success = true,
                TimeStamp = DateTime.UtcNow,
                Message = $"Success calling {monitorTcp.Name}, Response StatusCode: {monitorTcp.Response}"
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
                MonitorId = item.MonitorId,
                Service = monitorTcp.Name,
                Region = (int)monitorTcp?.MonitorRegion,
                Environment = (int)monitorTcp?.MonitorEnvironment,
                IP = monitorTcp?.IP,
                Port = monitorTcp?.Port,
                Success = false,
                TimeStamp = DateTime.UtcNow,
                Message = $"Error calling {monitorTcp?.Name}, Response StatusCode: {monitorTcp?.Response}"
            });
        }
    }

    public async Task HandleSuccessK8sNotifications(MonitorK8s monitorK8S, string response)
    {
        var notificationIdList = await _monitorNotificationRepository.GetMonitorNotifications(monitorK8S.MonitorId);

        _notificationLogger.LogInformation(
            $"sending success notification calling {monitorK8S.ClusterName}");

        foreach (var item in notificationIdList)
        {
            await _publishEndpoint.Publish<NotificationAlert>(new
            {
                NotificationId = item.NotificationId,
                MonitorId = item.MonitorId,
                Service = monitorK8S.Name,
                Region = (int)monitorK8S.MonitorRegion,
                Environment = (int)monitorK8S.MonitorEnvironment,
                ClusterName = monitorK8S.ClusterName,
                Success = true,
                TimeStamp = DateTime.UtcNow,
                Message = $"Success calling {monitorK8S.ClusterName}"
            });
        }
    }

    public async Task HandleFailedK8sNotifications(MonitorK8s monitorK8S, string response)
    {
        var notificationIdList = await _monitorNotificationRepository.GetMonitorNotifications(monitorK8S.MonitorId);

        _notificationLogger.LogInformation(
            $"sending notification Error calling {monitorK8S.ClusterName} Error Detail: {response}");

        foreach (var item in notificationIdList)
        {
            await _publishEndpoint.Publish<NotificationAlert>(new
            {
                NotificationId = item.NotificationId,
                MonitorId = item.MonitorId,
                Service = monitorK8S.Name,
                Region = (int)monitorK8S?.MonitorRegion,
                Environment = (int)monitorK8S?.MonitorEnvironment,
                ClusterName = monitorK8S?.ClusterName,
                Success = false,
                TimeStamp = DateTime.UtcNow,
                Message = $"Error calling {monitorK8S?.Name}, Error Detail: {response}"
            });
        }
    }
}