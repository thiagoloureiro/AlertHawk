using System.Text.Json.Serialization;

namespace AlertHawk.Metrics.API.Models;

public class GcpPriceRequest
{
    public string CurrencyCode { get; set; } = "USD";

    /// <summary>
    /// Cloud Billing service ID. Defaults to Compute Engine (6F81-5844-456A).
    /// </summary>
    public string? ServiceId { get; set; }

    /// <summary>
    /// GCP region (e.g. us-central1). Matched against SKU serviceRegions.
    /// </summary>
    public string? Region { get; set; }

    /// <summary>
    /// GCE machine type (e.g. n1-standard-2). Used to derive resourceGroup when not set explicitly.
    /// </summary>
    public string? MachineType { get; set; }

    /// <summary>
    /// SKU category resource group (e.g. N1, E2). Derived from MachineType when omitted.
    /// </summary>
    public string? ResourceGroup { get; set; }

    /// <summary>
    /// SKU category usage type (e.g. OnDemand, Preemptible). Defaults to OnDemand.
    /// </summary>
    public string? UsageType { get; set; } = "OnDemand";

    /// <summary>
    /// Optional substring match on SKU description (case-insensitive).
    /// </summary>
    public string? DescriptionContains { get; set; }

    /// <summary>
    /// SKU category resource family. Defaults to Compute.
    /// </summary>
    public string? ResourceFamily { get; set; } = "Compute";

    /// <summary>
    /// Maximum API pages to fetch (5000 SKUs per page). Defaults to 10.
    /// </summary>
    public int MaxPages { get; set; } = 10;
}

public class GcpPriceResponse
{
    public string CurrencyCode { get; set; } = "USD";
    public int Count { get; set; }
    public List<GcpPriceItem> Items { get; set; } = new();
    public string? NextPageToken { get; set; }
}

public class GcpPriceItem
{
    public string SkuId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ServiceDisplayName { get; set; } = string.Empty;
    public string ResourceFamily { get; set; } = string.Empty;
    public string ResourceGroup { get; set; } = string.Empty;
    public string UsageType { get; set; } = string.Empty;
    public List<string> ServiceRegions { get; set; } = new();
    public string CurrencyCode { get; set; } = string.Empty;
    public double UnitPrice { get; set; }
    public string UsageUnit { get; set; } = string.Empty;
    public string UsageUnitDescription { get; set; } = string.Empty;
    public DateTime? EffectiveTime { get; set; }
    public string PricingSummary { get; set; } = string.Empty;
}

// GCP Cloud Billing Catalog API response shapes

internal class GcpCatalogSkusResponse
{
    [JsonPropertyName("skus")]
    public List<GcpCatalogSku> Skus { get; set; } = new();

    [JsonPropertyName("nextPageToken")]
    public string? NextPageToken { get; set; }
}

internal class GcpCatalogSku
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("skuId")]
    public string SkuId { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("category")]
    public GcpCatalogSkuCategory? Category { get; set; }

    [JsonPropertyName("serviceRegions")]
    public List<string> ServiceRegions { get; set; } = new();

    [JsonPropertyName("pricingInfo")]
    public List<GcpCatalogPricingInfo> PricingInfo { get; set; } = new();
}

internal class GcpCatalogSkuCategory
{
    [JsonPropertyName("serviceDisplayName")]
    public string ServiceDisplayName { get; set; } = string.Empty;

    [JsonPropertyName("resourceFamily")]
    public string ResourceFamily { get; set; } = string.Empty;

    [JsonPropertyName("resourceGroup")]
    public string ResourceGroup { get; set; } = string.Empty;

    [JsonPropertyName("usageType")]
    public string UsageType { get; set; } = string.Empty;
}

internal class GcpCatalogPricingInfo
{
    [JsonPropertyName("effectiveTime")]
    public DateTime? EffectiveTime { get; set; }

    [JsonPropertyName("summary")]
    public string Summary { get; set; } = string.Empty;

    [JsonPropertyName("pricingExpression")]
    public GcpCatalogPricingExpression? PricingExpression { get; set; }
}

internal class GcpCatalogPricingExpression
{
    [JsonPropertyName("usageUnit")]
    public string UsageUnit { get; set; } = string.Empty;

    [JsonPropertyName("usageUnitDescription")]
    public string UsageUnitDescription { get; set; } = string.Empty;

    [JsonPropertyName("tieredRates")]
    public List<GcpCatalogTieredRate> TieredRates { get; set; } = new();
}

internal class GcpCatalogTieredRate
{
    [JsonPropertyName("startUsageAmount")]
    public double StartUsageAmount { get; set; }

    [JsonPropertyName("unitPrice")]
    public GcpCatalogMoney? UnitPrice { get; set; }
}

internal class GcpCatalogMoney
{
    [JsonPropertyName("currencyCode")]
    public string CurrencyCode { get; set; } = string.Empty;

    [JsonPropertyName("units")]
    public long Units { get; set; }

    [JsonPropertyName("nanos")]
    public int Nanos { get; set; }
}
