using FinOpsToolSample.Models;

namespace FinOpsToolSample.Services;

internal static class DataCollectionAppServicePlanSku
{
    internal static void ApplyToResource(ResourceInfo resource, string? skuName, string? tier, int? capacity)
    {
        resource.Properties["SKU"] = skuName ?? "Unknown";
        resource.Properties["Tier"] = tier ?? "Unknown";
        resource.Properties["Capacity"] = capacity ?? 0;
    }
}
