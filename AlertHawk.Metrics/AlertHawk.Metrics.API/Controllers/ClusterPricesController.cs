using AlertHawk.Metrics.API.Models;
using AlertHawk.Metrics.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sentry;

namespace AlertHawk.Metrics.API.Controllers;

[ApiController]
[Route("api/cluster-prices")]
public class ClusterPricesController : ControllerBase
{
    private readonly IClickHouseService _clickHouseService;
    private readonly ILogger<ClusterPricesController> _logger;

    public ClusterPricesController(IClickHouseService clickHouseService, ILogger<ClusterPricesController> logger)
    {
        _clickHouseService = clickHouseService;
        _logger = logger;
    }

    /// <summary>
    /// Get cluster prices based on provided filters
    /// </summary>
    /// <param name="clusterName">Optional cluster name filter</param>
    /// <param name="nodeName">Optional node name filter</param>
    /// <param name="region">Optional region filter</param>
    /// <param name="instanceType">Optional instance type filter</param>
    /// <param name="minutes">Number of minutes to look back (default: 1440 = 24 hours)</param>
    /// <returns>List of cluster prices</returns>
    [HttpGet]
    [Authorize]
    public async Task<ActionResult<List<ClusterPriceDto>>> GetClusterPrices(
        [FromQuery] string? clusterName = null,
        [FromQuery] string? nodeName = null,
        [FromQuery] string? region = null,
        [FromQuery] string? instanceType = null,
        [FromQuery] int? minutes = 1440)
    {
        try
        {
            var prices = await _clickHouseService.GetClusterPricesAsync(
                clusterName,
                nodeName,
                region,
                instanceType,
                minutes);
            
            return Ok(prices);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching cluster prices");
            SentrySdk.CaptureException(ex);
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get cluster prices for a specific cluster
    /// </summary>
    /// <param name="clusterName">Cluster name</param>
    /// <param name="nodeName">Optional node name filter</param>
    /// <param name="region">Optional region filter</param>
    /// <param name="instanceType">Optional instance type filter</param>
    /// <param name="minutes">Number of minutes to look back (default: 1440 = 24 hours)</param>
    /// <returns>List of cluster prices for the specified cluster</returns>
    [HttpGet("cluster/{clusterName}")]
    [Authorize]
    public async Task<ActionResult<List<ClusterPriceDto>>> GetClusterPricesByCluster(
        string clusterName,
        [FromQuery] string? nodeName = null,
        [FromQuery] string? region = null,
        [FromQuery] string? instanceType = null,
        [FromQuery] int? minutes = 1440)
    {
        try
        {
            var prices = await _clickHouseService.GetClusterPricesAsync(
                clusterName,
                nodeName,
                region,
                instanceType,
                minutes);
            
            return Ok(prices);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching cluster prices for cluster {ClusterName}", clusterName);
            SentrySdk.CaptureException(ex);
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get cluster prices for a specific node
    /// </summary>
    /// <param name="clusterName">Cluster name</param>
    /// <param name="nodeName">Node name</param>
    /// <param name="minutes">Number of minutes to look back (default: 1440 = 24 hours)</param>
    /// <returns>List of cluster prices for the specified node</returns>
    [HttpGet("cluster/{clusterName}/node/{nodeName}")]
    [Authorize]
    public async Task<ActionResult<List<ClusterPriceDto>>> GetClusterPricesByNode(
        string clusterName,
        string nodeName,
        [FromQuery] int? minutes = 1440)
    {
        try
        {
            var prices = await _clickHouseService.GetClusterPricesAsync(
                clusterName,
                nodeName,
                null,
                null,
                minutes);
            
            return Ok(prices);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching cluster prices for node {NodeName} in cluster {ClusterName}", 
                nodeName, clusterName);
            SentrySdk.CaptureException(ex);
            return StatusCode(500, new { error = ex.Message });
        }
    }
}
