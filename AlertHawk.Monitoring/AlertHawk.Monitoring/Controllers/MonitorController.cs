using AlertHawk.Monitoring.Domain.Entities;
using AlertHawk.Monitoring.Domain.Interfaces.Services;
using AlertHawk.Monitoring.Infrastructure.MonitorManager;
using AlertHawk.Monitoring.Infrastructure.Utils;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using Monitor = AlertHawk.Monitoring.Domain.Entities.Monitor;

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
        [ProducesResponseType(typeof(MonitorStatusDashboard), StatusCodes.Status200OK)]
        [HttpGet("monitorStatusDashboard")]
        public async Task<IActionResult> GetMonitorStatusDashboard()
        {
            var result = await _monitorService.GetMonitorStatusDashboard();
            return Ok(result);
        }

        [SwaggerOperation(Summary = "Retrieves detailed status for the current monitor Agent")]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        [HttpGet("monitorAgentStatus")]
        public IActionResult GetMonitorAgentStatus()
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

        [SwaggerOperation(Summary =
            "Retrieves a List of items to be Monitored by Monitor Group Ids (JWT Token required)")]
        [ProducesResponseType(typeof(IEnumerable<Domain.Entities.Monitor>), StatusCodes.Status200OK)]
        [HttpGet("monitorListByMonitorGroupIds")]
        public async Task<IActionResult> GetMonitorListByMonitorGroupIds()
        {
            var jwtToken = TokenUtils.GetJwtToken(Request.Headers["Authorization"].ToString());
            if (jwtToken == null) return BadRequest("Invalid Token");
            
            var result = await _monitorService.GetMonitorListByMonitorGroupIds(jwtToken);
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

        [SwaggerOperation(Summary = "Create a new monitor Http")]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        [HttpPost("createMonitorHttp")]
        public async Task<IActionResult> CreateMonitor([FromBody] MonitorHttp monitorHttp)
        {
            await _monitorService.CreateMonitor(monitorHttp);
            return Ok();
        }

        [SwaggerOperation(Summary = "Pause or resume the monitoring for the specified monitorId")]
        [HttpPut("pauseMonitor/{id}/{paused}")]
        public async Task<IActionResult> PauseMonitor(int id, bool paused)
        {
            await _monitorService.PauseMonitor(id, paused);
            return Ok();
        }

        [SwaggerOperation(Summary = "Retrieves Failure Count")]
        [ProducesResponseType(typeof(IEnumerable<MonitorAgent>), StatusCodes.Status200OK)]
        [HttpGet("getMonitorFailureCount/{days}")]
        public async Task<IActionResult> GetMonitorFailureCount(int days)
        {
            var result = await _monitorService.GetMonitorFailureCount(days);
            return Ok(result);
        }
    }
}