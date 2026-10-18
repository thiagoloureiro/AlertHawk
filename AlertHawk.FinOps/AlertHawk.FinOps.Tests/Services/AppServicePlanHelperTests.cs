using FinOpsToolSample.Models;
using FinOpsToolSample.Services;

namespace AlertHawk.FinOps.Tests.Services;

public class AppServicePlanHelperTests
{
    [Fact]
    public void FormatUnusedPlanWarning_IncludesPlanSkuAndLocation()
    {
        var message = AppServicePlanAnalysisMessages.FormatUnusedPlanWarning("plan-a", "P1v3", "eastus");

        Assert.Contains("plan-a", message);
        Assert.Contains("P1v3", message);
        Assert.Contains("eastus", message);
        Assert.Contains("UNUSED", message);
    }

    [Fact]
    public void FormatPlanWithApps_IncludesCount()
    {
        var message = AppServicePlanAnalysisMessages.FormatPlanWithApps("plan-b", 3);

        Assert.Contains("plan-b", message);
        Assert.Contains("3", message);
    }

    [Fact]
    public void ApplyToResource_WritesSkuTierAndCapacity()
    {
        var resource = new ResourceInfo { Name = "plan", Type = "Microsoft.Web/serverfarms" };

        DataCollectionAppServicePlanSku.ApplyToResource(resource, "S1", "Standard", 2);

        Assert.Equal("S1", resource.Properties["SKU"]);
        Assert.Equal("Standard", resource.Properties["Tier"]);
        Assert.Equal(2, resource.Properties["Capacity"]);
    }

    [Fact]
    public void ApplyToResource_NullSkuValues_UseDefaults()
    {
        var resource = new ResourceInfo { Name = "plan", Type = "Microsoft.Web/serverfarms" };

        DataCollectionAppServicePlanSku.ApplyToResource(resource, null, null, null);

        Assert.Equal("Unknown", resource.Properties["SKU"]);
        Assert.Equal("Unknown", resource.Properties["Tier"]);
        Assert.Equal(0, resource.Properties["Capacity"]);
    }
}
