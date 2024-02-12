using AlertHawk.Monitoring.Domain.Entities;
using AlertHawk.Monitoring.Domain.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

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

        [SwaggerOperation(Summary = "Retrieves the history of the Monitor, limited to 10k rows")]
        [ProducesResponseType(typeof(IEnumerable<MonitorHistory>), StatusCodes.Status200OK)]
        [HttpGet("MonitorHistory/{id}")]
        public async Task<IActionResult> GetMonitorHistory(int id)
        {
            var result = await _monitorService.GetMonitorHistory(id);
            return Ok(result);
        }

        [SwaggerOperation(Summary = "Retrieves the history of the Monitor, by Id and Number of Days")]
        [ProducesResponseType(typeof(IEnumerable<MonitorHistory>), StatusCodes.Status200OK)]
        [HttpGet("MonitorHistoryByIdDays/{id}/{days}")]
        public async Task<IActionResult> GetMonitorHistory(int id, int days)
        {
            var result = await _monitorService.GetMonitorHistory(id, days);
            return Ok(result);
        }

        [SwaggerOperation(Summary = "Retrieves dashboard details like uptime % and cert information")]
        [ProducesResponseType(typeof(MonitorDashboard), StatusCodes.Status200OK)]
        [HttpGet("MonitorDashboardData/{id}")]
        public async Task<IActionResult> GetMonitorDashboardData(int id)
        {
            var result = await _monitorService.GetMonitorDashboardData(id);
            return Ok(result);
        }

        [SwaggerOperation(Summary =
            "Retrieves dashboard details like uptime % and cert information for a list of monitors")]
        [ProducesResponseType(typeof(List<MonitorDashboard>), StatusCodes.Status200OK)]
        [HttpPost("MonitorDashboardDataList")]
        public IActionResult GetMonitorDashboardDataList([FromBody] List<int> ids)
        {
            var result = _monitorService.GetMonitorDashboardDataList(ids);
            return Ok(result);
        }

        [SwaggerOperation(Summary = "Delete all monitor History by number of days - this operation is irreversible!")]
        [HttpDelete]
        public async Task<IActionResult> DeleteMonitorHistory(int days)
        {
            await _monitorService.DeleteMonitorHistory(days);
            return Ok();
        }
    }
}