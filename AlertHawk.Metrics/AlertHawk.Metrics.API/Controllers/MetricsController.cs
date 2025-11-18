using AlertHawk.Metrics;
using Microsoft.AspNetCore.Mvc;

namespace AlertHawk.Metrics.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MetricsController : ControllerBase
{
    private readonly ClickHouseService _clickHouseService;

    public MetricsController(ClickHouseService clickHouseService)
    {
        _clickHouseService = clickHouseService;
    }

    /// <summary>
    /// Get metrics by namespace
    /// </summary>
    /// <param name="namespace">Optional namespace filter</param>
    /// <param name="hours">Number of hours to look back (default: 24)</param>
    /// <param name="limit">Maximum number of results (default: 100)</param>
    /// <returns>List of pod/container metrics</returns>
    [HttpGet("namespace")]
    public async Task<ActionResult<List<PodMetricDto>>> GetMetricsByNamespace(
        [FromQuery] string? @namespace = null,
        [FromQuery] int? hours = 24,
        [FromQuery] int limit = 100)
    {
        try
        {
            var metrics = await _clickHouseService.GetMetricsByNamespaceAsync(@namespace, hours, limit);
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
    /// <param name="hours">Number of hours to look back (default: 24)</param>
    /// <param name="limit">Maximum number of results (default: 100)</param>
    /// <returns>List of pod/container metrics for the namespace</returns>
    [HttpGet("namespace/{namespace}")]
    public async Task<ActionResult<List<PodMetricDto>>> GetMetricsByNamespaceName(
        string @namespace,
        [FromQuery] int? hours = 24,
        [FromQuery] int limit = 100)
    {
        try
        {
            var metrics = await _clickHouseService.GetMetricsByNamespaceAsync(@namespace, hours, limit);
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
    /// <param name="hours">Number of hours to look back (default: 24)</param>
    /// <param name="limit">Maximum number of results (default: 100)</param>
    /// <returns>List of node metrics</returns>
    [HttpGet("node")]
    public async Task<ActionResult<List<NodeMetricDto>>> GetNodeMetrics(
        [FromQuery] string? nodeName = null,
        [FromQuery] int? hours = 24,
        [FromQuery] int limit = 100)
    {
        try
        {
            var metrics = await _clickHouseService.GetNodeMetricsAsync(nodeName, hours, limit);
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
    /// <param name="hours">Number of hours to look back (default: 24)</param>
    /// <param name="limit">Maximum number of results (default: 100)</param>
    /// <returns>List of node metrics for the specified node</returns>
    [HttpGet("node/{nodeName}")]
    public async Task<ActionResult<List<NodeMetricDto>>> GetNodeMetricsByName(
        string nodeName,
        [FromQuery] int? hours = 24,
        [FromQuery] int limit = 100)
    {
        try
        {
            var metrics = await _clickHouseService.GetNodeMetricsAsync(nodeName, hours, limit);
            return Ok(metrics);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get PVC metrics
    /// </summary>
    /// <param name="namespace">Optional namespace filter</param>
    /// <param name="pvcName">Optional PVC name filter</param>
    /// <param name="hours">Number of hours to look back (default: 24)</param>
    /// <param name="limit">Maximum number of results (default: 100)</param>
    /// <returns>List of PVC metrics</returns>
    [HttpGet("pvc")]
    public async Task<ActionResult<List<PvcMetricDto>>> GetPvcMetrics(
        [FromQuery] string? @namespace = null,
        [FromQuery] string? pvcName = null,
        [FromQuery] int? hours = 24,
        [FromQuery] int limit = 100)
    {
        try
        {
            var metrics = await _clickHouseService.GetPvcMetricsAsync(@namespace, pvcName, hours, limit);
            return Ok(metrics);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }
}