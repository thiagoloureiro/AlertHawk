using AlertHawk.Monitoring.Domain.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;

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

        [HttpGet("monitorGroupList")]
        public async Task<IActionResult> GetMonitorGroupList()
        {
            var result = await _monitorGroupService.GetMonitorGroupList();
            return Ok(result);
        }
        
        [HttpGet("monitorGroup/{id}")]
        public async Task<IActionResult> GetMonitorGroupById(int id)
        {
            var result = await  _monitorGroupService.GetMonitorGroupById(id);
            return Ok(result);
        }
    }
}