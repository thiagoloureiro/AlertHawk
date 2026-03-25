using FinOpsToolSample.Services;

namespace AlertHawk.FinOps.Tests.Services;

public class AppServicePlanAnalysisServiceTests
{
    [Fact]
    public void FormatUnusedPlanWarning_IncludesPlanSkuAndLocation()
    {
        var line = AppServicePlanAnalysisMessages.FormatUnusedPlanWarning("plan-a", "P1v3", "East US");

        Assert.Equal(
            "⚠️ UNUSED: App Service Plan 'plan-a' has NO apps - SKU: P1v3, Location: East US",
            line);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    public void FormatPlanWithApps_IncludesPlanNameAndCount(int count)
    {
        var line = AppServicePlanAnalysisMessages.FormatPlanWithApps("my-plan", count);
        Assert.Equal($"✓ Plan 'my-plan' has {count} app(s)", line);
    }
}
