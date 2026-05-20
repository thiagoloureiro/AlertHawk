using AlertHawk.Metrics.API.Models;
using AlertHawk.Metrics.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sentry;

namespace AlertHawk.Metrics.API.Controllers;

[ApiController]
[Route("api/gcp-prices")]
[AllowAnonymous]
public class GcpPricesController : ControllerBase
{
    private readonly IGcpPricesService _gcpPricesService;

    public GcpPricesController(IGcpPricesService gcpPricesService)
    {
        _gcpPricesService = gcpPricesService;
    }

    /// <summary>
    /// Get GCP prices from the Cloud Billing Catalog API based on provided filters.
    /// </summary>
    /// <param name="request">GCP price request with filters (region, machine type, etc.)</param>
    /// <returns>GCP price response with matching SKU items</returns>
    [HttpPost]
    public async Task<ActionResult<GcpPriceResponse>> GetGcpPrices([FromBody] GcpPriceRequest? request)
    {
        try
        {
            if (request == null)
            {
                return BadRequest(new { error = "Request body is required" });
            }

            var response = await _gcpPricesService.GetPricesAsync(request);
            return Ok(response);
        }
        catch (Exception ex)
        {
            SentrySdk.CaptureException(ex);
            return StatusCode(500, new { error = ex.Message });
        }
    }
}
