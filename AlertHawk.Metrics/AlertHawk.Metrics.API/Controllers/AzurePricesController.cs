using AlertHawk.Metrics.API.Models;
using AlertHawk.Metrics.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sentry;

namespace AlertHawk.Metrics.API.Controllers;

[ApiController]
[Route("api/azure-prices")]
public class AzurePricesController : ControllerBase
{
    private readonly IAzurePricesService _azurePricesService;

    public AzurePricesController(IAzurePricesService azurePricesService)
    {
        _azurePricesService = azurePricesService;
    }

    /// <summary>
    /// Get Azure prices based on provided filters
    /// </summary>
    /// <param name="request">Azure price request with filters</param>
    /// <returns>Azure price response with items</returns>
    [HttpPost]
    [Authorize]
    public async Task<ActionResult<AzurePriceResponse>> GetAzurePrices([FromBody] AzurePriceRequest request)
    {
        try
        {
            if (request == null)
            {
                return BadRequest(new { error = "Request body is required" });
            }

            var response = await _azurePricesService.GetPricesAsync(request);
            return Ok(response);
        }
        catch (Exception ex)
        {
            SentrySdk.CaptureException(ex);
            return StatusCode(500, new { error = ex.Message });
        }
    }
}

