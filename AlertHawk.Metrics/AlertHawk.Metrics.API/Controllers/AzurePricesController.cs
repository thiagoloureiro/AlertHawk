using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AlertHawk.Metrics.API.Models;
using AlertHawk.Metrics.API.Services;
using EasyMemoryCache;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sentry;

namespace AlertHawk.Metrics.API.Controllers;

[ApiController]
[Route("api/azure-prices")]
[AllowAnonymous]
public class AzurePricesController : ControllerBase
{
    private readonly IAzurePricesService _azurePricesService;
    private readonly ICaching _caching;

    public AzurePricesController(IAzurePricesService azurePricesService, ICaching caching)
    {
        _azurePricesService = azurePricesService;
        _caching = caching;
    }

    /// <summary>
    /// Get Azure prices based on provided filters
    /// </summary>
    /// <param name="request">Azure price request with filters</param>
    /// <returns>Azure price response with items</returns>
    [HttpPost]
    public async Task<ActionResult<AzurePriceResponse>> GetAzurePrices([FromBody] AzurePriceRequest? request)
    {
        try
        {
            if (request == null)
            {
                return BadRequest(new { error = "Request body is required" });
            }

            // Generate cache key from request
            var cacheKey = GenerateCacheKey(request);
            
            // If not in cache, fetch from service
            var response =
                await _caching.GetOrSetObjectFromCacheAsync(cacheKey, 60,
                    () => _azurePricesService.GetPricesAsync(request));
            
            return Ok(response);
        }
        catch (Exception ex)
        {
            SentrySdk.CaptureException(ex);
            return StatusCode(500, new { error = ex.Message });
        }
    }

    private string GenerateCacheKey(AzurePriceRequest request)
    {
        // Serialize request to JSON to create a unique key
        var json = JsonSerializer.Serialize(request, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
        
        // Create a hash of the JSON to use as cache key
        var bytes = Encoding.UTF8.GetBytes(json);
        var hash = SHA256.HashData(bytes);
        var hashString = Convert.ToHexString(hash);
        
        return $"azure_prices_{hashString}";
    }
}

