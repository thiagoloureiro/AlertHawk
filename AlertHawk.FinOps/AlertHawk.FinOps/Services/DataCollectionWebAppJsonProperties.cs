using System.Linq;
using System.Text.Json;
using FinOpsToolSample.Models;

namespace FinOpsToolSample.Services;

internal static class DataCollectionWebAppJsonProperties
{
    internal static void ApplySitePropertiesFromJson(JsonElement props, ResourceInfo appResource)
    {
        if (props.TryGetProperty("state", out var state))
        {
            appResource.Properties["State"] = state.GetString() ?? "Unknown";
        }

        if (props.TryGetProperty("serverFarmId", out var farmId))
        {
            var planName = farmId.GetString()?.Split('/').LastOrDefault();
            appResource.Properties["AppServicePlan"] = planName ?? "Unknown";
        }
    }
}
