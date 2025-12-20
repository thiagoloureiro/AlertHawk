using AlertHawk.Metrics.API.Entities;
using AlertHawk.Metrics.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace AlertHawk.Metrics.API.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class MetricsNotificationController : ControllerBase
{
    private readonly IMetricsNotificationService _metricsNotificationService;

    public MetricsNotificationController(IMetricsNotificationService metricsNotificationService)
    {
        _metricsNotificationService = metricsNotificationService;
    }

    /// <summary>
    /// Retrieves a List of all Notifications by Cluster
    /// </summary>
    /// <param name="clusterName">Cluster name</param>
    /// <returns>List of notifications for the cluster</returns>
    [SwaggerOperation(Summary = "Retrieves a List of all Notifications by Cluster")]
    [ProducesResponseType(typeof(IEnumerable<MetricsNotification>), StatusCodes.Status200OK)]
    [HttpGet("clusterNotifications/{clusterName}")]
    public async Task<IActionResult> GetMetricsNotification(string clusterName)
    {
        var normalizedClusterName = clusterName ?? string.Empty;
        var result = await _metricsNotificationService.GetMetricsNotifications(normalizedClusterName);
        return Ok(result);
    }

    /// <summary>
    /// Add Notification to Cluster
    /// </summary>
    /// <param name="metricsNotification">Metrics notification data</param>
    /// <returns>Success status</returns>
    [SwaggerOperation(Summary = "Add Notification to Cluster")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [HttpPost("addMetricsNotification")]
    public async Task<IActionResult> AddMetricsNotification([FromBody] MetricsNotification metricsNotification)
    {
        if (string.IsNullOrWhiteSpace(metricsNotification.ClusterName))
        {
            return BadRequest("ClusterName is required");
        }

        if (metricsNotification.NotificationId <= 0)
        {
            return BadRequest("NotificationId must be greater than 0");
        }

        var normalizedClusterName = metricsNotification.ClusterName ?? string.Empty;
        var notifications = await _metricsNotificationService.GetMetricsNotifications(normalizedClusterName);

        if (notifications.Any(x => x.NotificationId == metricsNotification.NotificationId))
        {
            return BadRequest("Notification already exists for this cluster");
        }

        metricsNotification.ClusterName = normalizedClusterName;
        await _metricsNotificationService.AddMetricsNotification(metricsNotification);
        return Ok();
    }

    /// <summary>
    /// Remove Notification from Cluster
    /// </summary>
    /// <param name="metricsNotification">Metrics notification data</param>
    /// <returns>Success status</returns>
    [SwaggerOperation(Summary = "Remove Notification from Cluster")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [HttpPost("removeMetricsNotification")]
    public async Task<IActionResult> RemoveMetricsNotification([FromBody] MetricsNotification metricsNotification)
    {
        if (string.IsNullOrWhiteSpace(metricsNotification.ClusterName))
        {
            return BadRequest("ClusterName is required");
        }

        if (metricsNotification.NotificationId <= 0)
        {
            return BadRequest("NotificationId must be greater than 0");
        }

        metricsNotification.ClusterName = metricsNotification.ClusterName ?? string.Empty;
        await _metricsNotificationService.RemoveMetricsNotification(metricsNotification);
        return Ok();
    }
}
