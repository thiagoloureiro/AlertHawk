using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AlertHawk.Metrics.API.Models;
using EasyMemoryCache;
using Microsoft.Extensions.Options;

namespace AlertHawk.Metrics.API.Services;

[ExcludeFromCodeCoverage]
public class GcpPricesService : IGcpPricesService
{
    private const string GcpBillingCatalogBaseUrl = "https://cloudbilling.googleapis.com/v1";
    private const int CacheExpirationMinutes = 60;

    private readonly HttpClient _httpClient;
    private readonly ICaching _caching;
    private readonly GcpBillingOptions _options;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public GcpPricesService(HttpClient httpClient, ICaching caching, IOptions<GcpBillingOptions> options)
    {
        _httpClient = httpClient;
        _caching = caching;
        _options = options.Value;
        _httpClient.Timeout = TimeSpan.FromSeconds(60);
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "AlertHawk/1.0.1");
        _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
    }

    public async Task<GcpPriceResponse> GetPricesAsync(GcpPriceRequest request)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            throw new InvalidOperationException(
                "GCP Billing API key is not configured. Set GCP_BILLING_API_KEY or GcpBilling:ApiKey.");
        }

        var cacheKey = GenerateCacheKey(request);
        return await _caching.GetOrSetObjectFromCacheAsync(cacheKey, CacheExpirationMinutes,
            () => GetPricesFromApiAsync(request));
    }

    private async Task<GcpPriceResponse> GetPricesFromApiAsync(GcpPriceRequest request)
    {
        try
        {
            var serviceId = string.IsNullOrWhiteSpace(request.ServiceId)
                ? _options.ComputeEngineServiceId
                : request.ServiceId;

            var resourceGroup = ResolveResourceGroup(request);
            var usageType = string.IsNullOrWhiteSpace(request.UsageType) ? "OnDemand" : request.UsageType;
            var maxPages = request.MaxPages <= 0 ? 10 : Math.Min(request.MaxPages, 50);

            var matchedItems = new List<GcpPriceItem>();
            string? pageToken = null;
            string? lastNextPageToken = null;
            var pagesFetched = 0;

            do
            {
                var url = BuildSkusUrl(serviceId, request.CurrencyCode, pageToken);
                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var jsonContent = await response.Content.ReadAsStringAsync();
                var catalogResponse = JsonSerializer.Deserialize<GcpCatalogSkusResponse>(jsonContent, JsonOptions);

                if (catalogResponse == null)
                {
                    throw new InvalidOperationException("Failed to deserialize GCP Billing Catalog API response");
                }

                foreach (var sku in catalogResponse.Skus)
                {
                    if (!MatchesFilters(sku, request, resourceGroup, usageType))
                    {
                        continue;
                    }

                    var item = MapToPriceItem(sku, request.CurrencyCode);
                    if (item != null)
                    {
                        matchedItems.Add(item);
                    }
                }

                lastNextPageToken = catalogResponse.NextPageToken;
                pageToken = catalogResponse.NextPageToken;
                pagesFetched++;
            } while (!string.IsNullOrWhiteSpace(pageToken) && pagesFetched < maxPages);

            return new GcpPriceResponse
            {
                CurrencyCode = request.CurrencyCode,
                Count = matchedItems.Count,
                Items = matchedItems,
                NextPageToken = lastNextPageToken
            };
        }
        catch (Exception ex) when (ex is not InvalidOperationException)
        {
            throw new InvalidOperationException($"Error calling GCP Billing Catalog API: {ex.Message}", ex);
        }
    }

    private string BuildSkusUrl(string serviceId, string currencyCode, string? pageToken)
    {
        var queryParams = new List<string>
        {
            $"key={Uri.EscapeDataString(_options.ApiKey!)}",
            $"currencyCode={Uri.EscapeDataString(currencyCode)}"
        };

        if (!string.IsNullOrWhiteSpace(pageToken))
        {
            queryParams.Add($"pageToken={Uri.EscapeDataString(pageToken)}");
        }

        var queryString = string.Join("&", queryParams);
        return $"{GcpBillingCatalogBaseUrl}/services/{serviceId}/skus?{queryString}";
    }

    private static bool MatchesFilters(
        GcpCatalogSku sku,
        GcpPriceRequest request,
        string? resourceGroup,
        string usageType)
    {
        var category = sku.Category;
        if (category == null)
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(request.ResourceFamily) &&
            !category.ResourceFamily.Equals(request.ResourceFamily, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(resourceGroup) &&
            !category.ResourceGroup.Equals(resourceGroup, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!category.UsageType.Equals(usageType, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(request.Region) &&
            !sku.ServiceRegions.Any(r => r.Equals(request.Region, StringComparison.OrdinalIgnoreCase)))
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(request.DescriptionContains) &&
            !sku.Description.Contains(request.DescriptionContains, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(request.MachineType))
        {
            var machineType = request.MachineType;

            if (sku.Description.Contains(machineType, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (machineType.Contains("standard", StringComparison.OrdinalIgnoreCase) &&
                !sku.Description.Contains("Predefined Instance", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (machineType.Contains("highcpu", StringComparison.OrdinalIgnoreCase) &&
                !sku.Description.Contains("Instance Core", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (machineType.Contains("highmem", StringComparison.OrdinalIgnoreCase) &&
                !sku.Description.Contains("Instance Ram", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
        }

        return true;
    }

    private static GcpPriceItem? MapToPriceItem(GcpCatalogSku sku, string requestedCurrency)
    {
        var pricingInfo = sku.PricingInfo.FirstOrDefault();
        var tieredRate = pricingInfo?.PricingExpression?.TieredRates.FirstOrDefault();
        var unitPrice = tieredRate?.UnitPrice;

        if (unitPrice == null)
        {
            return null;
        }

        return new GcpPriceItem
        {
            SkuId = sku.SkuId,
            Name = sku.Name,
            Description = sku.Description,
            ServiceDisplayName = sku.Category?.ServiceDisplayName ?? string.Empty,
            ResourceFamily = sku.Category?.ResourceFamily ?? string.Empty,
            ResourceGroup = sku.Category?.ResourceGroup ?? string.Empty,
            UsageType = sku.Category?.UsageType ?? string.Empty,
            ServiceRegions = sku.ServiceRegions,
            CurrencyCode = unitPrice.CurrencyCode,
            UnitPrice = ToDecimalPrice(unitPrice),
            UsageUnit = pricingInfo?.PricingExpression?.UsageUnit ?? string.Empty,
            UsageUnitDescription = pricingInfo?.PricingExpression?.UsageUnitDescription ?? string.Empty,
            EffectiveTime = pricingInfo?.EffectiveTime,
            PricingSummary = pricingInfo?.Summary ?? string.Empty
        };
    }

    private static double ToDecimalPrice(GcpCatalogMoney money)
        => money.Units + money.Nanos / 1_000_000_000.0;

    public static string? ResolveResourceGroup(GcpPriceRequest request)
    {
        if (!string.IsNullOrWhiteSpace(request.ResourceGroup))
        {
            return request.ResourceGroup;
        }

        if (string.IsNullOrWhiteSpace(request.MachineType))
        {
            return null;
        }

        var family = request.MachineType.Split('-')[0];
        if (string.IsNullOrWhiteSpace(family))
        {
            return null;
        }

        return char.ToUpperInvariant(family[0]) + family[1..].ToLowerInvariant();
    }

    private static string GenerateCacheKey(GcpPriceRequest request)
    {
        var json = JsonSerializer.Serialize(request, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        var bytes = Encoding.UTF8.GetBytes(json);
        var hash = SHA256.HashData(bytes);
        return $"gcp_prices_{Convert.ToHexString(hash)}";
    }
}
