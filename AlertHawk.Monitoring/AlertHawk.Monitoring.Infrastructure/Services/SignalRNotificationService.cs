using AlertHawk.Monitoring.Domain.Entities;
using AlertHawk.Monitoring.Domain.Interfaces.Services;
using AlertHawk.Monitoring.Infrastructure.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;

namespace AlertHawk.Monitoring.Infrastructure.Services;

[ExcludeFromCodeCoverage]
public class SignalRNotificationService : ISignalRNotificationService
{
    private readonly IHubContext<NotificationHub> _hubContext;
    private readonly ILogger<SignalRNotificationService> _logger;

    public SignalRNotificationService(IHubContext<NotificationHub> hubContext, ILogger<SignalRNotificationService> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task SendNotificationToMonitorGroupAsync(int monitorId, NotificationAlert notification)
    {
        try
        {
            await _hubContext.Clients.Group($"Monitor_{monitorId}").SendAsync("ReceiveNotification", notification);
            _logger.LogInformation($"Notification sent to Monitor group {monitorId} for Monitor {notification.MonitorId}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error sending notification to Monitor group {monitorId} for Monitor {notification.MonitorId}");
        }
    }

    public async Task SendNotificationToEnvironmentGroupAsync(int environment, NotificationAlert notification)
    {
        try
        {
            await _hubContext.Clients.Group($"Environment_{environment}").SendAsync("ReceiveNotification", notification);
            _logger.LogInformation($"Notification sent to Environment group {environment} for Monitor {notification.MonitorId}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error sending notification to Environment group {environment} for Monitor {notification.MonitorId}");
        }
    }

    public async Task SendNotificationToRegionGroupAsync(int region, NotificationAlert notification)
    {
        try
        {
            await _hubContext.Clients.Group($"Region_{region}").SendAsync("ReceiveNotification", notification);
            _logger.LogInformation($"Notification sent to Region group {region} for Monitor {notification.MonitorId}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error sending notification to Region group {region} for Monitor {notification.MonitorId}");
        }
    }

    public async Task SendNotificationToUserAsync(string connectionId, NotificationAlert notification)
    {
        try
        {
            await _hubContext.Clients.Client(connectionId).SendAsync("ReceiveNotification", notification);
            _logger.LogInformation($"Notification sent to user {connectionId} for Monitor {notification.MonitorId}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error sending notification to user {connectionId} for Monitor {notification.MonitorId}");
        }
    }
}
