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
    public class MonitorGroupController : ControllerBase
    {
        private readonly IMonitorGroupService _monitorGroupService;

        public MonitorGroupController(IMonitorGroupService monitorGroupService)
        {
            _monitorGroupService = monitorGroupService;
        }

        [SwaggerOperation(Summary = "Retrieves a List of all Monitor Groups")]
        [ProducesResponseType(typeof(IEnumerable<MonitorGroup>), StatusCodes.Status200OK)]
        [HttpGet("monitorGroupList")]
        [Authorize]
        public async Task<IActionResult> GetMonitorGroupList()
        {
            var result = await _monitorGroupService.GetMonitorGroupList();
            return Ok(result);
        }

        [SwaggerOperation(Summary =
            "Retrieves a List of all Monitor Groups (including monitor list + dashboard data) By User Token")]
        [ProducesResponseType(typeof(IEnumerable<MonitorGroup>), StatusCodes.Status200OK)]
        [HttpGet("monitorGroupListByUser")]
        [Authorize]
        public async Task<IActionResult> GetMonitorGroupListByUser()
        {
            var jwtToken = TokenUtils.GetJwtToken(Request.Headers["Authorization"].ToString());
            if (jwtToken == null)
            {
                return BadRequest("Invalid Token");
            }

            var result = await _monitorGroupService.GetMonitorGroupList(jwtToken);
            return Ok(result);
        }

        [SwaggerOperation(Summary =
            "Retrieves a List of all Monitor Groups (including monitor list + dashboard data) By User Token")]
        [ProducesResponseType(typeof(IEnumerable<MonitorGroup>), StatusCodes.Status200OK)]
        [HttpGet("monitorGroupListByUser/{environment}")]
        [Authorize]
        public async Task<IActionResult> GetMonitorGroupListByEnvironment(
            MonitorEnvironment environment = MonitorEnvironment.Production)
        {
            var jwtToken = TokenUtils.GetJwtToken(Request.Headers["Authorization"].ToString());
            if (jwtToken == null)
            {
                return BadRequest("Invalid Token");
            }

            var result = await _monitorGroupService.GetMonitorGroupListByEnvironment(jwtToken, environment);
            return Ok(result);
        }

        [SwaggerOperation(Summary = "Retrieves a Monitor Group By Id")]
        [ProducesResponseType(typeof(MonitorGroup), StatusCodes.Status200OK)]
        [HttpGet("monitorGroup/{id}")]
        public async Task<IActionResult> GetMonitorGroupById(int id)
        {
            var result = await _monitorGroupService.GetMonitorGroupById(id);
            return Ok(result);
        }

        [SwaggerOperation(Summary = "Add Monitors to a Group")]
        [ProducesResponseType(typeof(MonitorGroup), StatusCodes.Status200OK)]
        [HttpPost("addMonitorToGroup")]
        public async Task<IActionResult> AddMonitorToGroup([FromBody] MonitorGroupItems monitorGroupItems)
        {
            await _monitorGroupService.AddMonitorToGroup(monitorGroupItems);
            return Ok();
        }

        [SwaggerOperation(Summary = "Create a Monitor Group")]
        [ProducesResponseType(typeof(MonitorGroup), StatusCodes.Status200OK)]
        [HttpPost("addMonitorGroup")]
        public async Task<IActionResult> AddMonitorGroup([FromBody] MonitorGroup monitorGroup)
        {
            await _monitorGroupService.AddMonitorGroup(monitorGroup);
            return Ok();
        }

        [SwaggerOperation(Summary = "Update a Monitor Group")]
        [ProducesResponseType(typeof(MonitorGroup), StatusCodes.Status200OK)]
        [HttpPost("updateMonitorGroup")]
        public async Task<IActionResult> UpdateMonitorGroup([FromBody] MonitorGroup monitorGroup)
        {
            await _monitorGroupService.UpdateMonitorGroup(monitorGroup);
            return Ok();
        }

        [SwaggerOperation(Summary = "Delete a Monitor Group")]
        [ProducesResponseType(typeof(MonitorGroup), StatusCodes.Status200OK)]
        [HttpDelete("deleteMonitorGroup/{id}")]
        public async Task<IActionResult> DeleteMonitorGroup(int id)
        {
            var jwtToken = TokenUtils.GetJwtToken(Request.Headers["Authorization"].ToString());
            if (jwtToken == null)
            {
                return BadRequest("Invalid Token");
            }

            var monitorGroup = await _monitorGroupService.GetMonitorGroupById(id);
            if (monitorGroup.Id == 0)
            {
                return BadRequest("monitorGroups.monitorNotFound");
            }
            else if(monitorGroup.Monitors.Any())
            {
                return BadRequest("monitorGroups.hasItemsFound");
            }

            await _monitorGroupService.DeleteMonitorGroup(jwtToken, id);
            return Ok();
        }

        [SwaggerOperation(Summary = "Remove Monitors from the Group")]
        [ProducesResponseType(typeof(MonitorGroup), StatusCodes.Status200OK)]
        [HttpDelete("removeMonitorFromGroup")]
        public async Task<IActionResult> RemoveMonitorFromGroup([FromBody] MonitorGroupItems monitorGroupItems)
        {
            await _monitorGroupService.RemoveMonitorFromGroup(monitorGroupItems);
            return Ok();
        }
    }
}