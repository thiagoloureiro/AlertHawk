using AlertHawk.Monitoring.Domain.Entities;
using AlertHawk.Monitoring.Domain.Interfaces.Services;
using AlertHawk.Monitoring.Infrastructure.MonitorManager;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace AlertHawk.Monitoring.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MonitorController : ControllerBase
    {
        private readonly IMonitorService _monitorService;
        private readonly IMonitorAgentService _monitorAgentService;

        public MonitorController(IMonitorService monitorService, IMonitorAgentService monitorAgentService)
        {
            _monitorService = monitorService;
            _monitorAgentService = monitorAgentService;
        }

        [SwaggerOperation(Summary = "Retrieves detailed status for the current monitor Agent")]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        [HttpGet("monitorStatus")]
        public IActionResult MonitorStatus()
        {
            return Ok(
                $"Master Node: {GlobalVariables.MasterNode}, MonitorId: {GlobalVariables.NodeId}, HttpTasksList Count: {GlobalVariables.HttpTaskList?.Count()}, TcpTasksList Count: {GlobalVariables.TcpTaskList?.Count()}");
        }

        [SwaggerOperation(Summary = "Retrieves a List of items to be Monitored")]
        [ProducesResponseType(typeof(IEnumerable<Domain.Entities.Monitor>), StatusCodes.Status200OK)]
        [HttpGet("monitorList")]
        public async Task<IActionResult> GetMonitorList()
        {
            var result = await _monitorService.GetMonitorList();
            return Ok(result);
        }

        [SwaggerOperation(Summary = "Retrieves a List of all Monitor Agents")]
        [ProducesResponseType(typeof(IEnumerable<MonitorAgent>), StatusCodes.Status200OK)]
        [HttpGet("allMonitorAgents")]
        public async Task<IActionResult> GetAllMonitorAgents()
        {
            var result = await _monitorAgentService.GetAllMonitorAgents();
            return Ok(result);
        }

        [SwaggerOperation(Summary = "Retrieves a List of all Notifications by Monitor")]
        [ProducesResponseType(typeof(IEnumerable<MonitorNotification>), StatusCodes.Status200OK)]
        [HttpGet("monitorNotifications/{id}")]
        public async Task<IActionResult> GetMonitorNotification(int id)
        {
            var result = await _monitorService.GetMonitorNotifications(id);
            return Ok(result);
        }

        [SwaggerOperation(Summary = "Pause or resume the monitoring for the specified monitorId")]
        [HttpPut("pauseMonitor/{id}/{paused}")]
        public async Task<IActionResult> PauseMonitor(int id, bool paused)
        {
            await _monitorService.PauseMonitor(id, paused);
            return Ok();
        }
    }
}