using AlertHawk.Metrics.API.Models;

namespace AlertHawk.Metrics.API.Services;

public interface IGcpPricesService
{
    Task<GcpPriceResponse> GetPricesAsync(GcpPriceRequest request);
}
