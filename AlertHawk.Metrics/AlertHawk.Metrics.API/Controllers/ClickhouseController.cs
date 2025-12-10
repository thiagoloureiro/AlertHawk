using AlertHawk.Metrics.API.Models;
using AlertHawk.Metrics.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AlertHawk.Metrics.API.Controllers;

[ApiController]
[Route("api/clickhouse")]
public class ClickhouseController : ControllerBase
{
    private readonly IClickHouseService _clickHouseService;

    public ClickhouseController(IClickHouseService clickHouseService)
    {
        _clickHouseService = clickHouseService;
    }

    /// <summary>
    /// Get table sizes from ClickHouse system.parts
    /// </summary>
    /// <returns>List of tables with their sizes, ordered by size descending</returns>
    [HttpGet("table-sizes")]
    [Authorize]
    public async Task<ActionResult<List<TableSizeDto>>> GetTableSizes()
    {
        try
        {
            var tableSizes = await _clickHouseService.GetTableSizesAsync();
            return Ok(tableSizes);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }
}
