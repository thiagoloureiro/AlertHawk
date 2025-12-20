using AlertHawk.Metrics.API.Entities;
using AlertHawk.Metrics.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace AlertHawk.Metrics.API.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class MetricsAlertController : ControllerBase
{
    private readonly IMetricsAlertService _metricsAlertService;

    public MetricsAlertController(IMetricsAlertService metricsAlertService)
    {
        _metricsAlertService = metricsAlertService;
    }

    /// <summary>
    /// Retrieves a list of Metrics Alerts
    /// </summary>
    /// <param name="clusterName">Optional cluster name filter</param>
    /// <param name="nodeName">Optional node name filter</param>
    /// <param name="days">Number of days to look back (default: 30)</param>
    /// <returns>List of metrics alerts</returns>
    [SwaggerOperation(Summary = "Retrieves a list of Metrics Alerts")]
    [ProducesResponseType(typeof(List<MetricsAlert>), StatusCodes.Status200OK)]
    [HttpGet("metricsAlerts")]
    public async Task<IActionResult> GetMetricsAlerts(
        [FromQuery] string? clusterName = null,
        [FromQuery] string? nodeName = null,
        [FromQuery] int? days = 30)
    {
        var result = await _metricsAlertService.GetMetricsAlerts(clusterName, nodeName, days);
        return Ok(result);
    }

    /// <summary>
    /// Retrieves a list of Metrics Alerts by Cluster
    /// </summary>
    /// <param name="clusterName">Cluster name</param>
    /// <param name="days">Number of days to look back (default: 30)</param>
    /// <returns>List of metrics alerts for the cluster</returns>
    [SwaggerOperation(Summary = "Retrieves a list of Metrics Alerts by Cluster")]
    [ProducesResponseType(typeof(List<MetricsAlert>), StatusCodes.Status200OK)]
    [HttpGet("metricsAlerts/cluster/{clusterName}")]
    public async Task<IActionResult> GetMetricsAlertsByCluster(
        string clusterName,
        [FromQuery] int? days = 30)
    {
        var result = await _metricsAlertService.GetMetricsAlerts(clusterName, null, days);
        return Ok(result);
    }

    /// <summary>
    /// Retrieves a list of Metrics Alerts by Node
    /// </summary>
    /// <param name="clusterName">Cluster name</param>
    /// <param name="nodeName">Node name</param>
    /// <param name="days">Number of days to look back (default: 30)</param>
    /// <returns>List of metrics alerts for the node</returns>
    [SwaggerOperation(Summary = "Retrieves a list of Metrics Alerts by Node")]
    [ProducesResponseType(typeof(List<MetricsAlert>), StatusCodes.Status200OK)]
    [HttpGet("metricsAlerts/cluster/{clusterName}/node/{nodeName}")]
    public async Task<IActionResult> GetMetricsAlertsByNode(
        string clusterName,
        string nodeName,
        [FromQuery] int? days = 30)
    {
        var result = await _metricsAlertService.GetMetricsAlerts(clusterName, nodeName, days);
        return Ok(result);
    }
}
