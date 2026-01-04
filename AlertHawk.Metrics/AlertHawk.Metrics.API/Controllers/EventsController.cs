using AlertHawk.Metrics.API.Models;
using AlertHawk.Metrics.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sentry;

namespace AlertHawk.Metrics.API.Controllers;

[ApiController]
[Route("api/events")]
public class EventsController : ControllerBase
{
    private readonly IClickHouseService _clickHouseService;
    private readonly ILogger<EventsController> _logger;
    
    public EventsController(
        IClickHouseService clickHouseService,
        ILogger<EventsController> logger)
    {
        _clickHouseService = clickHouseService;
        _logger = logger;
    }

    /// <summary>
    /// Write Kubernetes event
    /// </summary>
    /// <param name="request">Kubernetes event data</param>
    /// <returns>Success status</returns>
    [HttpPost]
    [AllowAnonymous]
    public async Task<ActionResult> WriteKubernetesEvent([FromBody] KubernetesEventRequest request)
    {
        try
        {
            var clusterName = !string.IsNullOrWhiteSpace(request.ClusterName)
                ? request.ClusterName
                : null;

            await _clickHouseService.WriteKubernetesEventAsync(
                request.Namespace,
                request.EventName,
                request.EventUid,
                request.InvolvedObjectKind,
                request.InvolvedObjectName,
                request.InvolvedObjectNamespace,
                request.EventType,
                request.Reason,
                request.Message,
                request.SourceComponent,
                request.Count,
                request.FirstTimestamp,
                request.LastTimestamp,
                clusterName);
            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error writing Kubernetes event");
            SentrySdk.CaptureException(ex);
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get Kubernetes events
    /// </summary>
    /// <param name="namespace">Optional namespace filter</param>
    /// <param name="involvedObjectKind">Optional involved object kind filter (e.g., Pod, Node)</param>
    /// <param name="involvedObjectName">Optional involved object name filter</param>
    /// <param name="eventType">Optional event type filter (Normal, Warning)</param>
    /// <param name="minutes">Number of minutes to look back (default: 1440 = 24 hours)</param>
    /// <param name="limit">Maximum number of results (default: 1000)</param>
    /// <param name="clusterName">Optional cluster name filter</param>
    /// <returns>List of Kubernetes events</returns>
    [HttpGet]
    [Authorize]
    public async Task<ActionResult<List<KubernetesEventDto>>> GetKubernetesEvents(
        [FromQuery] string? @namespace = null,
        [FromQuery] string? involvedObjectKind = null,
        [FromQuery] string? involvedObjectName = null,
        [FromQuery] string? eventType = null,
        [FromQuery] int? minutes = 1440,
        [FromQuery] int limit = 1000,
        [FromQuery] string? clusterName = null)
    {
        try
        {
            var events = await _clickHouseService.GetKubernetesEventsAsync(
                @namespace,
                involvedObjectKind,
                involvedObjectName,
                eventType,
                minutes,
                limit,
                clusterName);
            return Ok(events);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving Kubernetes events");
            SentrySdk.CaptureException(ex);
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get Kubernetes events for a specific namespace
    /// </summary>
    /// <param name="namespace">Namespace name</param>
    /// <param name="involvedObjectKind">Optional involved object kind filter</param>
    /// <param name="involvedObjectName">Optional involved object name filter</param>
    /// <param name="eventType">Optional event type filter</param>
    /// <param name="minutes">Number of minutes to look back (default: 1440 = 24 hours)</param>
    /// <param name="limit">Maximum number of results (default: 1000)</param>
    /// <param name="clusterName">Optional cluster name filter</param>
    /// <returns>List of Kubernetes events for the namespace</returns>
    [HttpGet("namespace/{namespace}")]
    [Authorize]
    public async Task<ActionResult<List<KubernetesEventDto>>> GetKubernetesEventsByNamespace(
        string @namespace,
        [FromQuery] string? involvedObjectKind = null,
        [FromQuery] string? involvedObjectName = null,
        [FromQuery] string? eventType = null,
        [FromQuery] int? minutes = 1440,
        [FromQuery] int limit = 1000,
        [FromQuery] string? clusterName = null)
    {
        try
        {
            var events = await _clickHouseService.GetKubernetesEventsAsync(
                @namespace,
                involvedObjectKind,
                involvedObjectName,
                eventType,
                minutes,
                limit,
                clusterName);
            return Ok(events);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving Kubernetes events for namespace {Namespace}", @namespace);
            SentrySdk.CaptureException(ex);
            return StatusCode(500, new { error = ex.Message });
        }
    }
}

