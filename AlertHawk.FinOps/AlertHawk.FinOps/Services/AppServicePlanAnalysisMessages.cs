namespace FinOpsToolSample.Services;

/// <summary>
/// Console messages for <see cref="AppServicePlanAnalysisService"/> (unit-tested).
/// </summary>
internal static class AppServicePlanAnalysisMessages
{
    internal static string FormatUnusedPlanWarning(string planName, string skuName, string location) =>
        $"⚠️ UNUSED: App Service Plan '{planName}' has NO apps - SKU: {skuName}, Location: {location}";

    internal static string FormatPlanWithApps(string planName, int appCount) =>
        $"✓ Plan '{planName}' has {appCount} app(s)";
}
