using AlertHawk.Monitoring.Domain.Entities;
using AlertHawk.Monitoring.Domain.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace AlertHawk.Monitoring.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
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
        public async Task<IActionResult> GetMonitorGroupList()
        {
            var result = await _monitorGroupService.GetMonitorGroupList();
            return Ok(result);
        }
        
        [SwaggerOperation(Summary = "Retrieves a Monitor Group By Id")]
        [ProducesResponseType(typeof(MonitorGroup), StatusCodes.Status200OK)]
        [HttpGet("monitorGroup/{id}")]
        public async Task<IActionResult> GetMonitorGroupById(int id)
        {
            var result = await  _monitorGroupService.GetMonitorGroupById(id);
            return Ok(result);
        }
    }
}