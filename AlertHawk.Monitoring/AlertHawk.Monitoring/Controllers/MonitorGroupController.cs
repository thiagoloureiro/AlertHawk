using AlertHawk.Monitoring.Domain.Interfaces.Services;
using AlertHawk.Monitoring.Infrastructure.MonitorManager;
using MassTransit;
using Microsoft.AspNetCore.Mvc;
using SharedModels;

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