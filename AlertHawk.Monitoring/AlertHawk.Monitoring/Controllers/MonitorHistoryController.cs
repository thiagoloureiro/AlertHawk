using AlertHawk.Monitoring.Domain.Entities;
using AlertHawk.Monitoring.Domain.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace AlertHawk.Monitoring.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class MonitorHistoryController : ControllerBase
    {
        private readonly IMonitorService _monitorService;
        private readonly IMonitorHistoryService _monitorHistoryService;


        public MonitorHistoryController(IMonitorService monitorService, IMonitorHistoryService monitorHistoryService)
        {
            _monitorService = monitorService;
            _monitorHistoryService = monitorHistoryService;
        }

        [SwaggerOperation(Summary = "Retrieves the history of the Monitor, limited to 10k rows")]
        [ProducesResponseType(typeof(IEnumerable<MonitorHistory>), StatusCodes.Status200OK)]
        [HttpGet("MonitorHistory/{id}")]
        public async Task<IActionResult> GetMonitorHistory(int id)
        {
            var result = await _monitorHistoryService.GetMonitorHistory(id);
            return Ok(result);
        }

        [SwaggerOperation(Summary = "Retrieves the history of the Monitor, by Id and Number of Days")]
        [ProducesResponseType(typeof(IEnumerable<MonitorHistory>), StatusCodes.Status200OK)]
        [HttpGet("MonitorHistoryByIdDays/{id}/{days}")]
        public async Task<IActionResult> GetMonitorHistory(int id, int days)
        {
            var result = await _monitorHistoryService.GetMonitorHistory(id, days);
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
            await _monitorHistoryService.DeleteMonitorHistory(days);
            return Ok();
        }

        [SwaggerOperation(Summary = "Monitor History count")]
        [ProducesResponseType(typeof(long), StatusCodes.Status200OK)]
        [HttpGet("GetMonitorHistoryCount")]
        public async Task<IActionResult> GetMonitorHistoryCount()
        {
            var result = await _monitorHistoryService.GetMonitorHistoryCount();
            return Ok(result);
        }
        
        [SwaggerOperation(Summary = "Get Monitor History Retention")]
        [ProducesResponseType(typeof(MonitorSettings), StatusCodes.Status200OK)]
        [HttpGet("GetMonitorHistoryRetention")]
        public async Task<IActionResult> GetMonitorHistoryRetention()
        {
            var result = await _monitorHistoryService.GetMonitorHistoryRetention();
            return Ok(result);
        }

        [SwaggerOperation(Summary = "Set Monitor History Retention")]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        [HttpPost("SetMonitorHistoryRetention")]
        public async Task<IActionResult> SetMonitorHistoryRetention([FromBody] MonitorSettings settings)
        {
            await _monitorHistoryService.SetMonitorHistoryRetention(settings.HistoryDaysRetention);
            return Ok();
        }
    }
}