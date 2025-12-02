using System.Text.Json.Serialization;

namespace AlertHawk.Metrics.API.Models;

public class AzurePriceRequest
{
    public string CurrencyCode { get; set; } = "USD";
    public string? ServiceName { get; set; }
    public string? ArmSkuName { get; set; }
    public string? SkuName { get; set; }
    public string? ProductName { get; set; }
    public string? ArmRegionName { get; set; }
    public string? Type { get; set; }
    public string? Filter { get; set; } // Optional: if provided, will be used directly instead of building from other fields
}

public class AzurePriceItem
{
    [JsonPropertyName("currencyCode")]
    public string CurrencyCode { get; set; } = string.Empty;
    
    [JsonPropertyName("tierMinimumUnits")]
    public double TierMinimumUnits { get; set; }
    
    [JsonPropertyName("retailPrice")]
    public double RetailPrice { get; set; }
    
    [JsonPropertyName("unitPrice")]
    public double UnitPrice { get; set; }
    
    [JsonPropertyName("armRegionName")]
    public string ArmRegionName { get; set; } = string.Empty;
    
    [JsonPropertyName("location")]
    public string Location { get; set; } = string.Empty;
    
    [JsonPropertyName("effectiveStartDate")]
    public DateTime EffectiveStartDate { get; set; }
    
    [JsonPropertyName("meterId")]
    public string? MeterId { get; set; }
    
    [JsonPropertyName("meterName")]
    public string MeterName { get; set; } = string.Empty;
    
    [JsonPropertyName("productId")]
    public string ProductId { get; set; } = string.Empty;
    
    [JsonPropertyName("skuId")]
    public string SkuId { get; set; } = string.Empty;
    
    [JsonPropertyName("productName")]
    public string ProductName { get; set; } = string.Empty;
    
    [JsonPropertyName("skuName")]
    public string SkuName { get; set; } = string.Empty;
    
    [JsonPropertyName("serviceName")]
    public string ServiceName { get; set; } = string.Empty;
    
    [JsonPropertyName("serviceId")]
    public string ServiceId { get; set; } = string.Empty;
    
    [JsonPropertyName("serviceFamily")]
    public string ServiceFamily { get; set; } = string.Empty;
    
    [JsonPropertyName("unitOfMeasure")]
    public string UnitOfMeasure { get; set; } = string.Empty;
    
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;
    
    [JsonPropertyName("isPrimaryMeterRegion")]
    public bool IsPrimaryMeterRegion { get; set; }
    
    [JsonPropertyName("armSkuName")]
    public string ArmSkuName { get; set; } = string.Empty;
    
    [JsonPropertyName("effectiveEndDate")]
    public DateTime? EffectiveEndDate { get; set; }
}

public class AzurePriceResponse
{
    [JsonPropertyName("BillingCurrency")]
    public string BillingCurrency { get; set; } = string.Empty;
    
    [JsonPropertyName("CustomerEntityId")]
    public string CustomerEntityId { get; set; } = string.Empty;
    
    [JsonPropertyName("CustomerEntityType")]
    public string CustomerEntityType { get; set; } = string.Empty;
    
    [JsonPropertyName("Items")]
    public List<AzurePriceItem> Items { get; set; } = new();
    
    [JsonPropertyName("NextPageLink")]
    public string? NextPageLink { get; set; }
    
    [JsonPropertyName("Count")]
    public int Count { get; set; }
}

