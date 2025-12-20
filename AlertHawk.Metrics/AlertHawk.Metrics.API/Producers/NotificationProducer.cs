using AlertHawk.Metrics.API.Repositories;
using MassTransit;
using Microsoft.Extensions.Logging;
using SharedModels;
using System.Diagnostics.CodeAnalysis;

namespace AlertHawk.Metrics.API.Producers;

[ExcludeFromCodeCoverage]
public class NotificationProducer : INotificationProducer
{
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly IMetricsNotificationRepository _metricsNotificationRepository;
    private readonly ILogger<NotificationProducer> _logger;

    public NotificationProducer(
        IPublishEndpoint publishEndpoint,
        IMetricsNotificationRepository metricsNotificationRepository,
        ILogger<NotificationProducer> logger)
    {
        _publishEndpoint = publishEndpoint;
        _metricsNotificationRepository = metricsNotificationRepository;
        _logger = logger;
    }

    public async Task SendNodeStatusNotification(
        string nodeName,
        string? clusterName,
        string? clusterEnvironment,
        bool? isReady,
        bool? hasMemoryPressure,
        bool? hasDiskPressure,
        bool? hasPidPressure,
        bool success)
    {
        try
        {
            var issues = new List<string>();
            
            if (isReady == false)
            {
                issues.Add("Node is not ready");
            }
            
            if (hasMemoryPressure == true)
            {
                issues.Add("Memory pressure detected");
            }
            
            if (hasDiskPressure == true)
            {
                issues.Add("Disk pressure detected");
            }
            
            if (hasPidPressure == true)
            {
                issues.Add("PID pressure detected");
            }

            var message = success
                ? $"Node {nodeName} is healthy. All conditions are OK."
                : $"Node {nodeName} has issues: {string.Join(", ", issues)}";

            var clusterInfo = !string.IsNullOrWhiteSpace(clusterName) ? $" (Cluster: {clusterName})" : "";
            var normalizedClusterName = clusterName ?? string.Empty;
            
            _logger.LogInformation(
                $"Sending node status notification for {nodeName}{clusterInfo}. Success: {success}, Message: {message}");

            var notificationIdList = await _metricsNotificationRepository.GetMetricsNotifications(normalizedClusterName);

            foreach (var item in notificationIdList)
            {
                _logger.LogInformation(
                    $"Notification Details: notificationId {item.NotificationId}, clusterName: {item.ClusterName}, Success: {success}");

                await _publishEndpoint.Publish<NotificationAlert>(new
                {
                    NotificationId = item.NotificationId,
                    MonitorId = 0, // Node metrics don't have a monitor ID
                    Service = $"Node: {nodeName}",
                    Region = 0, // Node metrics don't have a region mapping
                    Environment = 0, // Node metrics don't have an environment mapping
                    URL = string.Empty,
                    Success = success,
                    TimeStamp = DateTime.UtcNow,
                    Message = message,
                    StatusCode = success ? 200 : 500,
                    ReasonPhrase = string.Empty,
                    ClusterName = normalizedClusterName,
                    IP = string.Empty,
                    Port = string.Empty,
                    NodeName = nodeName,
                    ClusterEnvironment = clusterEnvironment ?? string.Empty,
                    IsReady = isReady,
                    HasMemoryPressure = hasMemoryPressure,
                    HasDiskPressure = hasDiskPressure,
                    HasPidPressure = hasPidPressure
                });

                _logger.LogInformation("Node status notification sent successfully");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error sending node status notification: {ex.Message}");
            SentrySdk.CaptureException(ex);
        }
    }
}