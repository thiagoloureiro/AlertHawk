using AlertHawk.Metrics.API.Entities;
using AlertHawk.Metrics.API.Repositories;
using Microsoft.Extensions.Logging;
using Rebus.Bus;
using SharedModels;
using System.Diagnostics.CodeAnalysis;

namespace AlertHawk.Metrics.API.Producers;

[ExcludeFromCodeCoverage]
public class NotificationProducer : INotificationProducer
{
    private readonly IBus _bus;
    private readonly IMetricsNotificationRepository _metricsNotificationRepository;
    private readonly IMetricsAlertRepository _metricsAlertRepository;
    private readonly ILogger<NotificationProducer> _logger;

    public NotificationProducer(
        IBus bus,
        IMetricsNotificationRepository metricsNotificationRepository,
        IMetricsAlertRepository metricsAlertRepository,
        ILogger<NotificationProducer> logger)
    {
        _bus = bus;
        _metricsNotificationRepository = metricsNotificationRepository;
        _metricsAlertRepository = metricsAlertRepository;
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

                await _bus.Send(new NotificationAlertMessage
                {
                    NotificationId = item.NotificationId,
                    MonitorId = 0,
                    Service = $"Node: {nodeName}",
                    Region = 0,
                    Environment = 0,
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

            // Save alert to database
            try
            {
                var metricsAlert = new MetricsAlert
                {
                    NodeName = nodeName,
                    ClusterName = normalizedClusterName,
                    TimeStamp = DateTime.UtcNow,
                    Status = success,
                    Message = message
                };

                await _metricsAlertRepository.SaveMetricsAlert(metricsAlert);
                _logger.LogInformation("Metrics alert saved to database successfully");
            }
            catch (Exception alertEx)
            {
                _logger.LogError($"Error saving metrics alert to database: {alertEx.Message}");
                SentrySdk.CaptureException(alertEx);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error sending node status notification: {ex.Message}");
            SentrySdk.CaptureException(ex);
        }
    }
}