using AlertHawk.Monitoring.Domain.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;

namespace AlertHawk.Monitoring.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MonitorHistoryController : ControllerBase
    {
        private readonly IMonitorService _monitorService;

        public MonitorHistoryController(IMonitorService monitorService)
        {
            _monitorService = monitorService;
        }

        [HttpGet]
        public async Task<IActionResult> GetMonitorHistory(int id)
        {
            var result = await _monitorService.GetMonitorHistory(id);
            return Ok(result);
        }
        
        [HttpDelete]
        public async Task<IActionResult> DeleteMonitorHistory(int days)
        {
            await _monitorService.DeleteMonitorHistory(days);
            return Ok();
        }
    }
}