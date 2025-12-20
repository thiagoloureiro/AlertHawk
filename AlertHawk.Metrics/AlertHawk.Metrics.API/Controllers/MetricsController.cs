using AlertHawk.Metrics.API.Models;
using AlertHawk.Metrics.API.Producers;
using AlertHawk.Metrics.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AlertHawk.Metrics.API.Controllers;

[ApiController]
[Route("api/metrics")]
public class MetricsController : ControllerBase
{
    private readonly IClickHouseService _clickHouseService;
    private readonly NodeStatusTracker _nodeStatusTracker;
    private readonly INotificationProducer _notificationProducer;

    public MetricsController(
        IClickHouseService clickHouseService,
        NodeStatusTracker nodeStatusTracker,
        INotificationProducer notificationProducer)
    {
        _clickHouseService = clickHouseService;
        _nodeStatusTracker = nodeStatusTracker;
        _notificationProducer = notificationProducer;
    }

    /// <summary>
    /// Get metrics by namespace
    /// </summary>
    /// <param name="namespace">Optional namespace filter</param>
    /// <param name="minutes">Number of minutes to look back (default: 1440 = 24 hours)</param>
    /// <param name="clusterName"></param>
    /// <returns>List of pod/container metrics</returns>
    [HttpGet("namespace")]
    [Authorize]
    public async Task<ActionResult<List<PodMetricDto>>> GetMetricsByNamespace(
        [FromQuery] string? @namespace = null,
        [FromQuery] int? minutes = 1440,
        [FromQuery] string? clusterName = null)
    {
        try
        {
            var metrics = await _clickHouseService.GetMetricsByNamespaceAsync(@namespace, minutes, clusterName);
            return Ok(metrics);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get metrics for a specific namespace
    /// </summary>
    /// <param name="namespace">Namespace name</param>
    /// <param name="minutes">Number of minutes to look back (default: 1440 = 24 hours)</param>
    /// <param name="clusterName"></param>
    /// <returns>List of pod/container metrics for the namespace</returns>
    [HttpGet("namespace/{namespace}")]
    [Authorize]
    public async Task<ActionResult<List<PodMetricDto>>> GetMetricsByNamespaceName(
        string @namespace,
        [FromQuery] int? minutes = 1440,
        [FromQuery] string? clusterName = null)
    {
        try
        {
            var metrics = await _clickHouseService.GetMetricsByNamespaceAsync(@namespace, minutes, clusterName);
            return Ok(metrics);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get node metrics
    /// </summary>
    /// <param name="nodeName">Optional node name filter</param>
    /// <param name="minutes">Number of minutes to look back (default: 1440 = 24 hours)</param>
    /// <param name="clusterName"></param>
    /// <returns>List of node metrics</returns>
    [HttpGet("node")]
    [Authorize]
    public async Task<ActionResult<List<NodeMetricDto>>> GetNodeMetrics(
        [FromQuery] string? nodeName = null,
        [FromQuery] int? minutes = 1440,
        [FromQuery] string? clusterName = null)
    {
        try
        {
            var metrics = await _clickHouseService.GetNodeMetricsAsync(nodeName, minutes, clusterName);
            return Ok(metrics);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get metrics for a specific node
    /// </summary>
    /// <param name="nodeName">Node name</param>
    /// <param name="minutes">Number of minutes to look back (default: 1440 = 24 hours)</param>
    /// <param name="clusterName"></param>
    /// <returns>List of node metrics for the specified node</returns>
    [HttpGet("node/{nodeName}")]
    [Authorize]
    public async Task<ActionResult<List<NodeMetricDto>>> GetNodeMetricsByName(
        string nodeName,
        [FromQuery] int? minutes = 1440,
        [FromQuery] string? clusterName = null)
    {
        try
        {
            var metrics = await _clickHouseService.GetNodeMetricsAsync(nodeName, minutes, clusterName);
            return Ok(metrics);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Write pod/container metrics
    /// </summary>
    /// <param name="request">Pod metric data</param>
    /// <returns>Success status</returns>
    [HttpPost("pod")]
    [AllowAnonymous]
    public async Task<ActionResult> WritePodMetric([FromBody] PodMetricRequest request)
    {
        try
        {
            var clusterName = !string.IsNullOrWhiteSpace(request.ClusterName)
                ? request.ClusterName
                : null;

            await _clickHouseService.WriteMetricsAsync(
                request.Namespace,
                request.Pod,
                request.Container,
                request.CpuUsageCores,
                request.CpuLimitCores,
                request.MemoryUsageBytes,
                clusterName,
                request.NodeName,
                request.PodState,
                request.RestartCount,
                request.PodAge);
            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Write node metrics
    /// </summary>
    /// <param name="request">Node metric data</param>
    /// <returns>Success status</returns>
    [HttpPost("node")]
    [AllowAnonymous]
    public async Task<ActionResult> WriteNodeMetric([FromBody] NodeMetricRequest request)
    {
        try
        {
            var clusterName = !string.IsNullOrWhiteSpace(request.ClusterName)
                ? request.ClusterName
                : null;

            await _clickHouseService.WriteNodeMetricsAsync(
                request.NodeName,
                request.CpuUsageCores,
                request.CpuCapacityCores,
                request.MemoryUsageBytes,
                request.MemoryCapacityBytes,
                clusterName,
                request.ClusterEnvironment,
                request.KubernetesVersion,
                request.CloudProvider,
                request.IsReady,
                request.HasMemoryPressure,
                request.HasDiskPressure,
                request.HasPidPressure,
                request.Architecture,
                request.OperatingSystem,
                request.Region,
                request.InstanceType);

            // Check for status changes and send notifications
            var nodeKey = _nodeStatusTracker.GetNodeKey(request.NodeName, clusterName);
            var hasStatusChanged = _nodeStatusTracker.HasStatusChanged(
                nodeKey,
                request.IsReady,
                request.HasMemoryPressure,
                request.HasDiskPressure,
                request.HasPidPressure,
                out var previousStatus);

            if (hasStatusChanged)
            {
                // Determine if the node is healthy (all conditions are OK)
                var isHealthy = (request.IsReady == true || request.IsReady == null) &&
                               (request.HasMemoryPressure == false || request.HasMemoryPressure == null) &&
                               (request.HasDiskPressure == false || request.HasDiskPressure == null) &&
                               (request.HasPidPressure == false || request.HasPidPressure == null);

                // Send notification for both OK and not OK status changes
                await _notificationProducer.SendNodeStatusNotification(
                    request.NodeName,
                    clusterName,
                    request.ClusterEnvironment,
                    request.IsReady,
                    request.HasMemoryPressure,
                    request.HasDiskPressure,
                    request.HasPidPressure,
                    isHealthy);
            }

            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            SentrySdk.CaptureException(ex);
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get unique cluster names from both node and namespace tables
    /// </summary>
    /// <returns>List of unique cluster names</returns>
    [HttpGet("clusters")]
    [Authorize]
    public async Task<ActionResult<List<string>>> GetUniqueClusterNames()
    {
        try
        {
            var clusterNames = await _clickHouseService.GetUniqueClusterNamesAsync();
           
            // Order alphabetically
            var orderedClusterNames = clusterNames.OrderBy(name => name).ToList();
            
            return Ok(orderedClusterNames);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get unique namespace names from the metrics table
    /// </summary>
    /// <param name="clusterName">Optional cluster name filter</param>
    /// <returns>List of unique namespace names</returns>
    [HttpGet("namespaces")]
    [Authorize]
    public async Task<ActionResult<List<string>>> GetUniqueNamespaceNames([FromQuery] string? clusterName = null)
    {
        try
        {
            var namespaceNames = await _clickHouseService.GetUniqueNamespaceNamesAsync(clusterName);
          
            // Order alphabetically
            var orderedNamespaces = namespaceNames.OrderBy(name => name).ToList();
            
            return Ok(orderedNamespaces);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Clean up metrics tables
    /// </summary>
    /// <param name="days">Number of days of retention. If 0, truncates both tables.</param>
    /// <returns>Success status with information about the cleanup operation</returns>
    [HttpDelete("cleanup")]
    [Authorize]
    public async Task<ActionResult> CleanupMetrics([FromQuery] int days = 0)
    {
        try
        {
            await _clickHouseService.CleanupMetricsAsync(days);
            var message = days == 0
                ? "All three tables (k8s_metrics, k8s_node_metrics, and k8s_pod_logs) have been truncated."
                : $"Records older than {days} days have been deleted from all three tables (k8s_metrics, k8s_node_metrics, and k8s_pod_logs).";
            return Ok(new { success = true, message });
        }
        catch (Exception ex)
        {
            SentrySdk.CaptureException(ex);
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Write pod logs
    /// </summary>
    /// <param name="request">Pod log data</param>
    /// <returns>Success status</returns>
    [HttpPost("pod/log")]
    [AllowAnonymous]
    public async Task<ActionResult> WritePodLog([FromBody] PodLogRequest request)
    {
        try
        {
            var clusterName = !string.IsNullOrWhiteSpace(request.ClusterName)
                ? request.ClusterName
                : null;

            await _clickHouseService.WritePodLogAsync(
                request.Namespace,
                request.Pod,
                request.Container,
                request.LogContent,
                clusterName);
            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            SentrySdk.CaptureException(ex);
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get pod logs
    /// </summary>
    /// <param name="namespace">Optional namespace filter</param>
    /// <param name="pod">Optional pod name filter</param>
    /// <param name="container">Optional container name filter</param>
    /// <param name="minutes">Number of minutes to look back (default: 1440 = 24 hours)</param>
    /// <param name="limit">Maximum number of results (default: 100)</param>
    /// <param name="clusterName">Optional cluster name filter</param>
    /// <returns>List of pod logs</returns>
    [HttpGet("pod/log")]
    [Authorize]
    public async Task<ActionResult<List<PodLogDto>>> GetPodLogs(
        [FromQuery] string? @namespace = null,
        [FromQuery] string? pod = null,
        [FromQuery] string? container = null,
        [FromQuery] int? minutes = 1440,
        [FromQuery] int limit = 100,
        [FromQuery] string? clusterName = null)
    {
        try
        {
            var logs = await _clickHouseService.GetPodLogsAsync(@namespace, pod, container, minutes, limit, clusterName);
            return Ok(logs);
        }
        catch (Exception ex)
        {
            SentrySdk.CaptureException(ex);
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get pod logs for a specific namespace
    /// </summary>
    /// <param name="namespace">Namespace name</param>
    /// <param name="pod">Optional pod name filter</param>
    /// <param name="container">Optional container name filter</param>
    /// <param name="minutes">Number of minutes to look back (default: 1440 = 24 hours)</param>
    /// <param name="limit">Maximum number of results (default: 100)</param>
    /// <param name="clusterName">Optional cluster name filter</param>
    /// <returns>List of pod logs for the namespace</returns>
    [HttpGet("pod/log/namespace/{namespace}")]
    [Authorize]
    public async Task<ActionResult<List<PodLogDto>>> GetPodLogsByNamespace(
        string @namespace,
        [FromQuery] string? pod = null,
        [FromQuery] string? container = null,
        [FromQuery] int? minutes = 1440,
        [FromQuery] int limit = 100,
        [FromQuery] string? clusterName = null)
    {
        try
        {
            var logs = await _clickHouseService.GetPodLogsAsync(@namespace, pod, container, minutes, limit, clusterName);
            return Ok(logs);
        }
        catch (Exception ex)
        {
            SentrySdk.CaptureException(ex);
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get pod logs for a specific pod
    /// </summary>
    /// <param name="namespace">Namespace name</param>
    /// <param name="pod">Pod name</param>
    /// <param name="container">Optional container name filter</param>
    /// <param name="minutes">Number of minutes to look back (default: 1440 = 24 hours)</param>
    /// <param name="limit">Maximum number of results (default: 100)</param>
    /// <param name="clusterName">Optional cluster name filter</param>
    /// <returns>List of pod logs for the specified pod</returns>
    [HttpGet("pod/log/namespace/{namespace}/pod/{pod}")]
    [Authorize]
    public async Task<ActionResult<List<PodLogDto>>> GetPodLogsByPod(
        string @namespace,
        string pod,
        [FromQuery] string? container = null,
        [FromQuery] int? minutes = 1440,
        [FromQuery] int limit = 100,
        [FromQuery] string? clusterName = null)
    {
        try
        {
            var logs = await _clickHouseService.GetPodLogsAsync(@namespace, pod, container, minutes, limit, clusterName);
            return Ok(logs);
        }
        catch (Exception ex)
        {
            SentrySdk.CaptureException(ex);
            return StatusCode(500, new { error = ex.Message });
        }
    }
}