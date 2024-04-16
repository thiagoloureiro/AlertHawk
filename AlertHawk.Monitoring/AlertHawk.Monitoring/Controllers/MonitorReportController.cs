using AlertHawk.Monitoring.Domain.Entities;
using AlertHawk.Monitoring.Domain.Entities.Report;
using AlertHawk.Monitoring.Domain.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace AlertHawk.Monitoring.Controllers;

[Route("api/[controller]")]
[ApiController]
[AllowAnonymous]
public class MonitorReportController: ControllerBase
{
    private IMonitorReportService _monitorReportService;
    
    public MonitorReportController(IMonitorReportService monitorReportService)
    {
        _monitorReportService = monitorReportService;
    }
    
    [SwaggerOperation(Summary = "Retrieves Total Online and Offline minutes by GroupId")]
    [ProducesResponseType(typeof(IEnumerable<MonitorReportUptime>), StatusCodes.Status200OK)]
    [HttpGet("Uptime/{groupId}/{hours}")]
    public async Task<IActionResult> GetMonitorReportUptime(int groupId, int hours)
    {
        var result = await _monitorReportService.GetMonitorReportUptime(groupId, hours);
        return Ok(result);
    }
    
    [SwaggerOperation(Summary = "Retrieves Total Monitor Alerts by GroupId")]
    [ProducesResponseType(typeof(IEnumerable<MonitorReportAlerts>), StatusCodes.Status200OK)]
    [HttpGet("Alert/{groupId}/{hours}")]
    public async Task<IActionResult> GetMonitorAlerts(int groupId, int hours)
    {
        var result = await _monitorReportService.GetMonitorAlerts(groupId, hours);
        return Ok(result);
    }
}