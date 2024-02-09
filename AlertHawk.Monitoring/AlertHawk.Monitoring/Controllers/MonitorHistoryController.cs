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

        // This will return only the last 10k rows of the monitor history
        [HttpGet("MonitorHistory/{id}")]
        public async Task<IActionResult> GetMonitorHistory(int id)
        {
            var result = await _monitorService.GetMonitorHistory(id);
            return Ok(result);
        }
        
        [HttpGet("MonitorHistoryByIdDays/{id}/{days}")]
        public async Task<IActionResult> GetMonitorHistory(int id, int days)
        {
            var result = await _monitorService.GetMonitorHistory(id, days);
            return Ok(result);
        }
        
        [HttpGet("MonitorDashboardData/{id}")]
        public async Task<IActionResult> GetMonitorDashboardData(int id)
        {
            var result = await _monitorService.GetMonitorDashboardData(id);
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