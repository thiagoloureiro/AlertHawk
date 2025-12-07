using AlertHawk.Metrics.API.Models;

namespace AlertHawk.Metrics.API.Services;

public interface IAzurePricesService
{
    Task<AzurePriceResponse> GetPricesAsync(AzurePriceRequest request);
}




