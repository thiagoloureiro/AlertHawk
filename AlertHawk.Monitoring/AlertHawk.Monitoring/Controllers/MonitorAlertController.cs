﻿using AlertHawk.Monitoring.Domain.Entities;
using AlertHawk.Monitoring.Domain.Interfaces.Services;
using AlertHawk.Monitoring.Infrastructure.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace AlertHawk.Monitoring.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class MonitorAlertController : ControllerBase
    {
        private readonly IMonitorAlertService _monitorAlertService;

        public MonitorAlertController(IMonitorAlertService monitorAlertService)
        {
            _monitorAlertService = monitorAlertService;
        }

        [SwaggerOperation(Summary = "Retrieves a list of Monitor Alerts")]
        [ProducesResponseType(typeof(List<MonitorAlert>), StatusCodes.Status200OK)]
        [HttpGet("monitorAlerts/{monitorId}/{days}")]
        public async Task<IActionResult> GetMonitorAlerts(int? monitorId = 0, int? days = 30, MonitorEnvironment? environment = MonitorEnvironment.All)
        {
            var jwtToken = TokenUtils.GetJwtToken(Request.Headers["Authorization"].ToString());
            if (jwtToken == null)
            {
                return BadRequest("Invalid Token");
            }

            var result = await _monitorAlertService.GetMonitorAlerts(monitorId, days, environment, jwtToken);
            return Ok(result);
        }

        [SwaggerOperation(Summary = "Retrieves a list of Monitor Alerts")]
        [ProducesResponseType(typeof(List<MonitorAlert>), StatusCodes.Status200OK)]
        [HttpGet("monitorAlerts/{monitorId}/{days}/{environment}")]
        public async Task<IActionResult> GetMonitorAlertsByEnvironment(int? monitorId = 0, int? days = 30, MonitorEnvironment? environment = MonitorEnvironment.All)
        {
            var jwtToken = TokenUtils.GetJwtToken(Request.Headers["Authorization"].ToString());
            if (jwtToken == null)
            {
                return BadRequest("Invalid Token");
            }

            var result = await _monitorAlertService.GetMonitorAlerts(monitorId, days, environment, jwtToken);
            return Ok(result);
        }

        [SwaggerOperation(Summary = "Retrieves a list of Monitor Alerts in Excel format")]
        [ProducesResponseType(typeof(List<MonitorAlert>), StatusCodes.Status200OK)]
        [HttpGet("monitorAlertsReport/{monitorId}/{days}/{environment}/{reportType}")]
        public async Task<IActionResult> GetMonitorAlertsReport(int? monitorId = 0, int? days = 30, MonitorEnvironment? environment = MonitorEnvironment.All,
            ReportType reportType = ReportType.Excel)
        {
            var jwtToken = TokenUtils.GetJwtToken(Request.Headers["Authorization"].ToString());
            if (jwtToken == null)
            {
                return BadRequest("Invalid Token");
            }

            var stream = await _monitorAlertService.GetMonitorAlertsReport(monitorId, days, jwtToken, environment, reportType);

            if (reportType == ReportType.Excel)
            {
                var fileName = $"MonitorAlerts_{DateTime.UtcNow:yyyyMMdd}.xlsx";
                return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    fileName);
            }

            return BadRequest("Invalid Report Type");
        }
        
        [SwaggerOperation(Summary = "Retrieves a list of Monitor Alerts by MonitorGroup")]
        [ProducesResponseType(typeof(List<MonitorAlert>), StatusCodes.Status200OK)]
        [HttpGet("monitorAlertsByMonitorGroup/{monitorGroupId}/{days}")]
        public async Task<IActionResult> GetMonitorAlertsByMonitorGroup(int monitorGroupId = 0, int? days = 30, MonitorEnvironment? environment = MonitorEnvironment.All)
        {
            var jwtToken = TokenUtils.GetJwtToken(Request.Headers["Authorization"].ToString());
            if (jwtToken == null)
            {
                return BadRequest("Invalid Token");
            }

            var result = await _monitorAlertService.GetMonitorAlertsByMonitorGroup(monitorGroupId, days, environment, jwtToken);
            return Ok(result);
        }
    }
}