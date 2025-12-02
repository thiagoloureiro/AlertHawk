using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.Json;
using AlertHawk.Metrics.API.Models;

namespace AlertHawk.Metrics.API.Services;

[ExcludeFromCodeCoverage]
public class AzurePricesService : IAzurePricesService
{
    private readonly HttpClient _httpClient;
    private const string AzurePricesApiBaseUrl = "https://prices.azure.com/api/retail/prices";

    public AzurePricesService(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _httpClient.Timeout = TimeSpan.FromSeconds(30);
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "AlertHawk/1.0.1");
        _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
    }

    public async Task<AzurePriceResponse> GetPricesAsync(AzurePriceRequest request)
    {
        try
        {
            var queryParams = new List<string>();

            // Add currency code (Azure API expects single quotes around the value, encoded as %27)
            if (!string.IsNullOrWhiteSpace(request.CurrencyCode))
            {
                var encodedCurrency = Uri.EscapeDataString($"'{request.CurrencyCode}'");
                queryParams.Add($"currencyCode={encodedCurrency}");
            }

            // Build filter if not provided directly
            string? filter = null;
            if (!string.IsNullOrWhiteSpace(request.Filter))
            {
                filter = request.Filter;
            }
            else
            {
                var filterParts = new List<string>();
                
                if (!string.IsNullOrWhiteSpace(request.ServiceName))
                {
                    filterParts.Add($"serviceName eq '{request.ServiceName}'");
                }
                
                if (!string.IsNullOrWhiteSpace(request.SkuName))
                {
                    filterParts.Add($"skuName eq '{request.SkuName}'");
                }
                
                if (!string.IsNullOrWhiteSpace(request.ProductName))
                {
                    filterParts.Add($"productName eq '{request.ProductName}'");
                }
                
                if (!string.IsNullOrWhiteSpace(request.ArmSkuName))
                {
                    filterParts.Add($"armSkuName eq '{request.ArmSkuName}'");
                }
                
                if (!string.IsNullOrWhiteSpace(request.ArmRegionName))
                {
                    filterParts.Add($"armRegionName eq '{request.ArmRegionName}'");
                }
                
                if (!string.IsNullOrWhiteSpace(request.Type))
                {
                    filterParts.Add($"type eq '{request.Type}'");
                }

                // Always exclude Spot and Low Priority SKUs
                filterParts.Add("contains(skuName, 'Spot') eq false");
                filterParts.Add("contains(skuName, 'Low Priority') eq false");

                if (filterParts.Any())
                {
                    filter = string.Join(" and ", filterParts);
                }
            }

            // Add filter to query params
            if (!string.IsNullOrWhiteSpace(filter))
            {
                queryParams.Add($"$filter={Uri.EscapeDataString(filter)}");
            }

            var queryString = string.Join("&", queryParams);
            var url = $"{AzurePricesApiBaseUrl}?{queryString}";

            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var jsonContent = await response.Content.ReadAsStringAsync();
            var azureResponse = JsonSerializer.Deserialize<AzurePriceResponse>(jsonContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (azureResponse == null)
            {
                throw new InvalidOperationException("Failed to deserialize Azure Prices API response");
            }

            return azureResponse;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error calling Azure Prices API: {ex.Message}", ex);
        }
    }
}

