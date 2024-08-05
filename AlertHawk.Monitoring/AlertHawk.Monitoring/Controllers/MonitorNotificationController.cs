using AlertHawk.Monitoring.Domain.Entities;
using AlertHawk.Monitoring.Domain.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace AlertHawk.Monitoring.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class MonitorNotificationController : ControllerBase
    {
        private readonly IMonitorNotificationService _monitorNotificationService;

        public MonitorNotificationController(IMonitorNotificationService monitorNotificationService)
        {
            _monitorNotificationService = monitorNotificationService;
        }

        [SwaggerOperation(Summary = "Retrieves a List of all Notifications by Monitor")]
        [ProducesResponseType(typeof(IEnumerable<MonitorNotification>), StatusCodes.Status200OK)]
        [HttpGet("monitorNotifications/{id}")]
        public async Task<IActionResult> GetMonitorNotification(int id)
        {
            var result = await _monitorNotificationService.GetMonitorNotifications(id);
            return Ok(result);
        }

        [SwaggerOperation(Summary = "Add Notification to Monitor")]
        [ProducesResponseType(typeof(IEnumerable<MonitorNotification>), StatusCodes.Status200OK)]
        [HttpPost("addMonitorNotification")]
        public async Task<IActionResult> AddMonitorNotification([FromBody] MonitorNotification monitorNotification)
        {
            var notifications = await _monitorNotificationService.GetMonitorNotifications(monitorNotification.MonitorId);

            if (notifications.Any(x => x.NotificationId == monitorNotification.NotificationId))
            {
                return BadRequest("Notification already exists for this monitor");
            }

            await _monitorNotificationService.AddMonitorNotification(monitorNotification);
            return Ok();
        }

        [SwaggerOperation(Summary = "Remove Notification from Monitor")]
        [ProducesResponseType(typeof(IEnumerable<MonitorNotification>), StatusCodes.Status200OK)]
        [HttpPost("removeMonitorNotification")]
        public async Task<IActionResult> RemoveMonitorNotification([FromBody] MonitorNotification monitorNotification)
        {
            await _monitorNotificationService.RemoveMonitorNotification(monitorNotification);
            return Ok();
        }

        [SwaggerOperation(Summary = "Add Notification to Monitor Group")]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        [HttpPost("addMonitorGroupNotification")]
        public async Task<IActionResult> AddMonitorGroupNotification(
            [FromBody] MonitorGroupNotification monitorGroupNotification)
        {
            await _monitorNotificationService.AddMonitorGroupNotification(monitorGroupNotification);
            return Ok();
        }

        [SwaggerOperation(Summary = "Remove Notification from Monitor Group")]
        [ProducesResponseType(typeof(IEnumerable<MonitorNotification>), StatusCodes.Status200OK)]
        [HttpPost("removeMonitorGroupNotification")]
        public async Task<IActionResult> RemoveMonitorGroupNotification(
            [FromBody] MonitorGroupNotification monitorGroupNotification)
        {
            await _monitorNotificationService.RemoveMonitorGroupNotification(monitorGroupNotification);
            return Ok();
        }
    }
}