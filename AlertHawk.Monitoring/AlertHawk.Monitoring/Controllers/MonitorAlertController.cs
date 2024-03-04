using AlertHawk.Monitoring.Domain.Entities;
using AlertHawk.Monitoring.Domain.Interfaces.Services;
using AlertHawk.Monitoring.Infrastructure.Utils;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace AlertHawk.Monitoring.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MonitorAlertController : ControllerBase
    {
        private readonly IMonitorAlertService _monitorAlertService;

        public MonitorAlertController(IMonitorAlertService monitorAlertService)
        {
            _monitorAlertService = monitorAlertService;
        }

        [SwaggerOperation(Summary = "Retrieves a list of Monitor Alerts")]
        [ProducesResponseType(typeof(MonitorStatusDashboard), StatusCodes.Status200OK)]
        [HttpGet("monitorAlerts/{monitorId}/{days}")]
        public async Task<IActionResult> GetMonitorAlerts(int? monitorId = 0, int? days = 30)
        {
            var jwtToken = TokenUtils.GetJwtToken(Request.Headers["Authorization"].ToString());
            if (jwtToken == null)
            {
                return BadRequest("Invalid Token");
            }

            var result = await _monitorAlertService.GetMonitorAlerts(monitorId, days, jwtToken);
            return Ok(result);
        }
    }
}