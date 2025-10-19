using AlertHawk.Monitoring.Domain.Entities;
using AlertHawk.Monitoring.Domain.Interfaces.Producers;
using AlertHawk.Monitoring.Domain.Interfaces.Repositories;
using AlertHawk.Monitoring.Domain.Interfaces.Services;
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
    private readonly ISignalRNotificationService _signalRNotificationService;

    public NotificationProducer(IPublishEndpoint publishEndpoint,
        IMonitorNotificationRepository monitorNotificationRepository, 
        ILogger<NotificationProducer> notificationLogger,
        ISignalRNotificationService signalRNotificationService)
    {
        _publishEndpoint = publishEndpoint;
        _monitorNotificationRepository = monitorNotificationRepository;
        _notificationLogger = notificationLogger;
        _signalRNotificationService = signalRNotificationService;
    }

    private AlertHawk.Monitoring.Domain.Interfaces.Services.NotificationAlert CreateNotificationAlert(MonitorHttp monitorHttp, bool success, string? reasonPhrase, int notificationId, int monitorId)
    {
        return new AlertHawk.Monitoring.Domain.Interfaces.Services.NotificationAlert
        {
            NotificationId = notificationId,
            MonitorId = monitorId,
            Service = monitorHttp.Name,
            Region = (int)monitorHttp?.MonitorRegion,
            Environment = (int)monitorHttp?.MonitorEnvironment,
            URL = monitorHttp?.UrlToCheck,
            Success = success,
            TimeStamp = DateTime.UtcNow,
            Message = success 
                ? $"Success calling {monitorHttp?.Name}, Response StatusCode: {monitorHttp?.ResponseStatusCode}"
                : $"Error calling {monitorHttp?.Name}, Response StatusCode: {monitorHttp?.ResponseStatusCode}",
            StatusCode = (int)monitorHttp.ResponseStatusCode,
            ReasonPhrase = reasonPhrase
        };
    }

    private AlertHawk.Monitoring.Domain.Interfaces.Services.NotificationAlert CreateTcpNotificationAlert(MonitorTcp monitorTcp, bool success, int notificationId, int monitorId)
    {
        return new AlertHawk.Monitoring.Domain.Interfaces.Services.NotificationAlert
        {
            NotificationId = notificationId,
            MonitorId = monitorId,
            Service = monitorTcp.Name,
            Region = (int)monitorTcp.MonitorRegion,
            Environment = (int)monitorTcp.MonitorEnvironment,
            IP = monitorTcp.IP,
            Port = monitorTcp.Port,
            Success = success,
            TimeStamp = DateTime.UtcNow,
            Message = success 
                ? $"Success calling {monitorTcp.Name}, Response StatusCode: {monitorTcp.Response}"
                : $"Error calling {monitorTcp?.Name}, Response StatusCode: {monitorTcp?.Response}"
        };
    }

    private AlertHawk.Monitoring.Domain.Interfaces.Services.NotificationAlert CreateK8sNotificationAlert(MonitorK8s monitorK8s, bool success, string response, int notificationId, int monitorId)
    {
        return new AlertHawk.Monitoring.Domain.Interfaces.Services.NotificationAlert
        {
            NotificationId = notificationId,
            MonitorId = monitorId,
            Service = monitorK8s.Name,
            Region = (int)monitorK8s.MonitorRegion,
            Environment = (int)monitorK8s.MonitorEnvironment,
            ClusterName = monitorK8s.ClusterName,
            Success = success,
            TimeStamp = DateTime.UtcNow,
            Message = success 
                ? $"Success calling {monitorK8s.ClusterName}"
                : $"Error calling {monitorK8s?.Name}, Error Detail: {response}"
        };
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
                
                // Create notification alert for both MassTransit and SignalR
                var notificationAlert = CreateNotificationAlert(monitorHttp, false, reasonPhrase, item.NotificationId, item.MonitorId);
                
                // Send via MassTransit
                await _publishEndpoint.Publish<SharedModels.NotificationAlert>(new
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

                // Send via SignalR (groups only)
                await _signalRNotificationService.SendNotificationToMonitorGroupAsync(monitorHttp.MonitorId, notificationAlert);
                await _signalRNotificationService.SendNotificationToEnvironmentGroupAsync((int)monitorHttp.MonitorEnvironment, notificationAlert);
                await _signalRNotificationService.SendNotificationToRegionGroupAsync((int)monitorHttp.MonitorRegion, notificationAlert);

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
            
            // Create notification alert for both MassTransit and SignalR
            var notificationAlert = CreateNotificationAlert(monitorHttp, true, reasonPhrase, item.NotificationId, item.MonitorId);
            
            // Send via MassTransit
            await _publishEndpoint.Publish<SharedModels.NotificationAlert>(new
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

            // Send via SignalR (groups only)
            await _signalRNotificationService.SendNotificationToMonitorGroupAsync(monitorHttp.MonitorId, notificationAlert);
            await _signalRNotificationService.SendNotificationToEnvironmentGroupAsync((int)monitorHttp.MonitorEnvironment, notificationAlert);
            await _signalRNotificationService.SendNotificationToRegionGroupAsync((int)monitorHttp.MonitorRegion, notificationAlert);
        }
    }

    public async Task HandleSuccessTcpNotifications(MonitorTcp monitorTcp)
    {
        var notificationIdList = await _monitorNotificationRepository.GetMonitorNotifications(monitorTcp.MonitorId);

        _notificationLogger.LogInformation(
            $"sending success notification calling {monitorTcp.IP} Port: {monitorTcp.Port},");

        foreach (var item in notificationIdList)
        {
            // Create notification alert for both MassTransit and SignalR
            var notificationAlert = CreateTcpNotificationAlert(monitorTcp, true, item.NotificationId, item.MonitorId);
            
            // Send via MassTransit
            await _publishEndpoint.Publish<SharedModels.NotificationAlert>(new
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

            // Send via SignalR (groups only)
            await _signalRNotificationService.SendNotificationToMonitorGroupAsync(monitorTcp.MonitorId, notificationAlert);
            await _signalRNotificationService.SendNotificationToEnvironmentGroupAsync((int)monitorTcp.MonitorEnvironment, notificationAlert);
            await _signalRNotificationService.SendNotificationToRegionGroupAsync((int)monitorTcp.MonitorRegion, notificationAlert);
        }
    }

    public async Task HandleFailedTcpNotifications(MonitorTcp monitorTcp)
    {
        var notificationIdList = await _monitorNotificationRepository.GetMonitorNotifications(monitorTcp.MonitorId);

        _notificationLogger.LogInformation(
            $"sending notification Error calling {monitorTcp.IP} Port: {monitorTcp.Port}, Response: {monitorTcp.Response}");

        foreach (var item in notificationIdList)
        {
            // Create notification alert for both MassTransit and SignalR
            var notificationAlert = CreateTcpNotificationAlert(monitorTcp, false, item.NotificationId, item.MonitorId);
            
            // Send via MassTransit
            await _publishEndpoint.Publish<SharedModels.NotificationAlert>(new
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

            // Send via SignalR (groups only)
            await _signalRNotificationService.SendNotificationToMonitorGroupAsync(monitorTcp.MonitorId, notificationAlert);
            await _signalRNotificationService.SendNotificationToEnvironmentGroupAsync((int)monitorTcp.MonitorEnvironment, notificationAlert);
            await _signalRNotificationService.SendNotificationToRegionGroupAsync((int)monitorTcp.MonitorRegion, notificationAlert);
        }
    }

    public async Task HandleSuccessK8sNotifications(MonitorK8s monitorK8S, string response)
    {
        var notificationIdList = await _monitorNotificationRepository.GetMonitorNotifications(monitorK8S.MonitorId);

        _notificationLogger.LogInformation(
            $"sending success notification calling {monitorK8S.ClusterName}");

        foreach (var item in notificationIdList)
        {
            // Create notification alert for both MassTransit and SignalR
            var notificationAlert = CreateK8sNotificationAlert(monitorK8S, true, response, item.NotificationId, item.MonitorId);
            
            // Send via MassTransit
            await _publishEndpoint.Publish<SharedModels.NotificationAlert>(new
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

            // Send via SignalR (groups only)
            await _signalRNotificationService.SendNotificationToMonitorGroupAsync(monitorK8S.MonitorId, notificationAlert);
            await _signalRNotificationService.SendNotificationToEnvironmentGroupAsync((int)monitorK8S.MonitorEnvironment, notificationAlert);
            await _signalRNotificationService.SendNotificationToRegionGroupAsync((int)monitorK8S.MonitorRegion, notificationAlert);
        }
    }

    public async Task HandleFailedK8sNotifications(MonitorK8s monitorK8S, string response)
    {
        var notificationIdList = await _monitorNotificationRepository.GetMonitorNotifications(monitorK8S.MonitorId);

        _notificationLogger.LogInformation(
            $"sending notification Error calling {monitorK8S.ClusterName} Error Detail: {response}");

        foreach (var item in notificationIdList)
        {
            // Create notification alert for both MassTransit and SignalR
            var notificationAlert = CreateK8sNotificationAlert(monitorK8S, false, response, item.NotificationId, item.MonitorId);
            
            // Send via MassTransit
            await _publishEndpoint.Publish<SharedModels.NotificationAlert>(new
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

            // Send via SignalR (groups only)
            await _signalRNotificationService.SendNotificationToMonitorGroupAsync(monitorK8S.MonitorId, notificationAlert);
            await _signalRNotificationService.SendNotificationToEnvironmentGroupAsync((int)monitorK8S.MonitorEnvironment, notificationAlert);
            await _signalRNotificationService.SendNotificationToRegionGroupAsync((int)monitorK8S.MonitorRegion, notificationAlert);
        }
    }
}