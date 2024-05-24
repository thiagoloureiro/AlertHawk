using AlertHawk.Monitoring.Domain.Classes;
using AlertHawk.Monitoring.Domain.Entities;
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
        [HttpGet("monitorStatusDashboard/{environment}")]
        public async Task<IActionResult> GetMonitorStatusDashboard(
            MonitorEnvironment environment = MonitorEnvironment.Production)
        {
            var jwtToken = TokenUtils.GetJwtToken(Request.Headers["Authorization"].ToString());
            if (string.IsNullOrEmpty(jwtToken))
            {
                return BadRequest("Invalid Token");
            }

            var result = await _monitorService.GetMonitorStatusDashboard(jwtToken, environment);
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

        [SwaggerOperation(Summary = "Retrieves a List of items to be Monitored, filtered by Tag")]
        [ProducesResponseType(typeof(IEnumerable<Domain.Entities.Monitor>), StatusCodes.Status200OK)]
        [HttpGet("monitorListByTag/{tag}")]
        public async Task<IActionResult> GetMonitorListByTag(string tag)
        {
            var result = await _monitorService.GetMonitorListByTag(tag);
            return Ok(result);
        }


        [SwaggerOperation(Summary = "Retrieves a List (string) of monitor Tags")]
        [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status200OK)]
        [HttpGet("monitorTagList")]
        public async Task<IActionResult> GetMonitorTagList()
        {
            var result = await _monitorService.GetMonitorTagList();
            return Ok(result);
        }


        [SwaggerOperation(Summary =
            "Retrieves a List of items to be Monitored by Monitor Group Ids (JWT Token required)")]
        [ProducesResponseType(typeof(IEnumerable<Domain.Entities.Monitor>), StatusCodes.Status200OK)]
        [HttpGet("monitorListByMonitorGroupIds/{environment}")]
        public async Task<IActionResult> GetMonitorListByMonitorGroupIds(
            MonitorEnvironment environment = MonitorEnvironment.Production)
        {
            var jwtToken = TokenUtils.GetJwtToken(Request.Headers["Authorization"].ToString());
            if (jwtToken == null)
            {
                return BadRequest("Invalid Token");
            }

            var result = await _monitorService.GetMonitorListByMonitorGroupIds(jwtToken, environment);
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
        
        [SwaggerOperation(Summary = "Create a new monitor Http")]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        [HttpPost("createMonitorHttp")]
        public async Task<IActionResult> CreateMonitorHttp([FromBody] MonitorHttp monitorHttp)
        {
            return Ok(await _monitorService.CreateMonitorHttp(monitorHttp));
        }

        [SwaggerOperation(Summary = "Update monitor Http")]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        [HttpPost("updateMonitorHttp")]
        public async Task<IActionResult> UpdateMonitorHttp([FromBody] MonitorHttp monitorHttp)
        {
            await _monitorService.UpdateMonitorHttp(monitorHttp);
            return Ok();
        }

        [SwaggerOperation(Summary = "Create a new monitor TCP")]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        [HttpPost("createMonitorTcp")]
        public async Task<IActionResult> CreateMonitorTcp([FromBody] MonitorTcp monitorTcp)
        {
            return Ok(await _monitorService.CreateMonitorTcp(monitorTcp));
        }

        [SwaggerOperation(Summary = "Update monitor TCP")]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        [HttpPost("updateMonitorTcp")]
        public async Task<IActionResult> UpdateMonitorTcp([FromBody] MonitorTcp monitorTcp)
        {
            await _monitorService.UpdateMonitorTcp(monitorTcp);
            return Ok();
        }

        [SwaggerOperation(Summary = "Delete Monitor")]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        [HttpDelete("deleteMonitor/{id}")]
        public async Task<IActionResult> DeleteMonitor(int id)
        {
            var jwtToken = TokenUtils.GetJwtToken(Request.Headers["Authorization"].ToString());
            await _monitorService.DeleteMonitor(id, jwtToken);
            return Ok();
        }

        [SwaggerOperation(Summary = "Pause or resume the monitoring for the specified monitorId")]
        [HttpPut("pauseMonitor/{id}/{paused}")]
        public async Task<IActionResult> PauseMonitor(int id, bool paused)
        {
            await _monitorService.PauseMonitor(id, paused);
            return Ok();
        }

        [SwaggerOperation(Summary = "Pause or resume the monitoring for the specified Monitor Group Id")]
        [HttpPut("pauseMonitorByGroupId/{groupId}/{paused}")]
        public async Task<IActionResult> PauseMonitorByGroupId(int groupId, bool paused)
        {
            await _monitorService.PauseMonitorByGroupId(groupId, paused);
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

        [SwaggerOperation(Summary = "Retrieves monitor http by monitorId")]
        [ProducesResponseType(typeof(MonitorHttp), StatusCodes.Status200OK)]
        [HttpGet("getMonitorHttpByMonitorId/{monitorId}")]
        public async Task<IActionResult> GetMonitorHttpByMonitorId(int monitorId)
        {
            var result = await _monitorService.GetHttpMonitorByMonitorId(monitorId);
            return Ok(result);
        }

        [SwaggerOperation(Summary = "Retrieves monitor tcp by monitorId")]
        [ProducesResponseType(typeof(MonitorTcp), StatusCodes.Status200OK)]
        [HttpGet("getMonitorTcpByMonitorId/{monitorId}")]
        public async Task<IActionResult> GetMonitorTcpByMonitorId(int monitorId)
        {
            var result = await _monitorService.GetTcpMonitorByMonitorId(monitorId);
            return Ok(result);
        }


        [SwaggerOperation(Summary = "Force Cache Refresh for Dashboard")]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        [HttpGet("forceCacheRefresh")]
        public async Task<IActionResult> ForceCashRefresh()
        {
            await _monitorService.SetMonitorDashboardDataCacheList();
            return Ok("OK");
        }
        
        [SwaggerOperation(Summary = "Monitor Count")]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        [HttpGet("GetMonitorCount")]
        public async Task<IActionResult> GetMonitorCount()
        {
            var monitorList = await _monitorService.GetMonitorList();
            return Ok(monitorList.Count());
        }
    }
}