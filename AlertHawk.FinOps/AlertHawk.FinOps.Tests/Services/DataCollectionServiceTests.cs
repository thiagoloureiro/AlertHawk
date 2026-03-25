using System.Text.Json;
using FinOpsToolSample.Models;
using FinOpsToolSample.Services;

namespace AlertHawk.FinOps.Tests.Services;

public class DataCollectionServiceTests
{
    [Fact]
    public void SetSubscriptionInfo_UpdatesCollectedData()
    {
        var svc = new DataCollectionService();

        svc.SetSubscriptionInfo("MySub", "sub-id-1");

        var data = svc.GetCollectedData();
        Assert.Equal("MySub", data.SubscriptionName);
        Assert.Equal("sub-id-1", data.SubscriptionId);
    }

    [Fact]
    public void SetCostData_UpdatesTotalsAndBreakdowns()
    {
        var svc = new DataCollectionService();
        var byRg = new Dictionary<string, decimal> { ["rg-a"] = 100 };
        var byService = new List<ServiceCostDetail>
        {
            new() { ServiceName = "Compute", ResourceGroup = "rg-a", Cost = 100 }
        };

        svc.SetCostData(250.5m, byRg, byService);

        var data = svc.GetCollectedData();
        Assert.Equal(250.5m, data.TotalMonthlyCost);
        Assert.Single(data.CostsByResourceGroup);
        Assert.Equal(100, data.CostsByResourceGroup["rg-a"]);
        Assert.Single(data.CostsByService);
        Assert.Equal("Compute", data.CostsByService[0].ServiceName);
    }

    [Fact]
    public void AddResource_AppendsToResourcesList()
    {
        var svc = new DataCollectionService();
        var r = new ResourceInfo { Type = "VM", Name = "vm1", ResourceGroup = "rg", Location = "eastus" };

        svc.AddResource(r);

        var data = svc.GetCollectedData();
        Assert.Single(data.Resources);
        Assert.Equal("vm1", data.Resources[0].Name);
    }

    public class AppServicePlanSku
    {
        [Fact]
        public void ApplyToResource_SetsSkuTierCapacity()
        {
            var r = new ResourceInfo { Type = "App Service Plan", Name = "p1" };
            DataCollectionAppServicePlanSku.ApplyToResource(r, "P1v3", "PremiumV3", 2);

            Assert.Equal("P1v3", r.Properties["SKU"]);
            Assert.Equal("PremiumV3", r.Properties["Tier"]);
            Assert.Equal(2, r.Properties["Capacity"]);
        }

        [Fact]
        public void ApplyToResource_NullsUseUnknownOrZero()
        {
            var r = new ResourceInfo { Type = "App Service Plan", Name = "p1" };
            DataCollectionAppServicePlanSku.ApplyToResource(r, null, null, null);

            Assert.Equal("Unknown", r.Properties["SKU"]);
            Assert.Equal("Unknown", r.Properties["Tier"]);
            Assert.Equal(0, r.Properties["Capacity"]);
        }
    }

    public class WebAppJsonProperties
    {
        [Fact]
        public void ApplySitePropertiesFromJson_SetsStateAndPlanNameFromServerFarmId()
        {
            using var doc = JsonDocument.Parse(
                """
                {
                  "state": "Running",
                  "serverFarmId": "/subscriptions/s/resourceGroups/rg/providers/Microsoft.Web/serverfarms/my-plan"
                }
                """);
            var app = new ResourceInfo { Type = "App Service", Name = "web1" };

            DataCollectionWebAppJsonProperties.ApplySitePropertiesFromJson(doc.RootElement, app);

            Assert.Equal("Running", app.Properties["State"]);
            Assert.Equal("my-plan", app.Properties["AppServicePlan"]);
        }
    }

    public class AppWebMonitoringMetrics
    {
        [Fact]
        public void ApplyMetric_Requests_SetsTotalOnly()
        {
            var r = new ResourceInfo { Type = "App Service", Name = "a" };
            DataCollectionAppWebMonitoringMetrics.ApplyMetric(r, "Requests", avgValue: 0, totalValue: 1200);

            Assert.Equal(1200, r.Metrics["Requests_Total"]);
            Assert.Empty(r.Flags);
        }

        [Fact]
        public void ApplyMetric_MemoryWorkingSet_ConvertsToMegabytes()
        {
            var r = new ResourceInfo { Type = "App Service", Name = "a" };
            var bytes = 3 * 1024 * 1024;
            DataCollectionAppWebMonitoringMetrics.ApplyMetric(r, "MemoryWorkingSet", bytes, 0);

            Assert.Equal(3, r.Metrics["Memory_Average_MB"]);
        }

        [Fact]
        public void ApplyMetric_Http5xx_AddsFlagWhenPositive()
        {
            var r = new ResourceInfo { Type = "App Service", Name = "a" };
            DataCollectionAppWebMonitoringMetrics.ApplyMetric(r, "Http5xx", 0, 4);

            Assert.Equal(4, r.Metrics["Http5xx_Errors_Total"]);
            Assert.Single(r.Flags);
            Assert.Contains("HTTP 5xx", r.Flags[0]);
        }

        [Fact]
        public void ApplyMetric_Http5xx_NoFlagWhenZero()
        {
            var r = new ResourceInfo { Type = "App Service", Name = "a" };
            DataCollectionAppWebMonitoringMetrics.ApplyMetric(r, "Http5xx", 0, 0);

            Assert.Equal(0, r.Metrics["Http5xx_Errors_Total"]);
            Assert.Empty(r.Flags);
        }

        [Theory]
        [InlineData(3.0, false)]
        [InlineData(3.01, true)]
        public void ApplyMetric_AverageResponseTime_FlagWhenAboveThreeSeconds(double avgSeconds, bool expectFlag)
        {
            var r = new ResourceInfo { Type = "App Service", Name = "a" };
            DataCollectionAppWebMonitoringMetrics.ApplyMetric(r, "AverageResponseTime", avgSeconds, 0);

            Assert.Equal(avgSeconds, r.Metrics["Response_Time_Average_Seconds"]);
            if (expectFlag)
            {
                Assert.Single(r.Flags);
                Assert.Contains("Slow response time", r.Flags[0]);
            }
            else
            {
                Assert.Empty(r.Flags);
            }
        }

        [Fact]
        public void ApplyMetric_UnknownName_DoesNothing()
        {
            var r = new ResourceInfo { Type = "App Service", Name = "a" };
            DataCollectionAppWebMonitoringMetrics.ApplyMetric(r, "CustomMetric", 1, 2);

            Assert.Empty(r.Metrics);
            Assert.Empty(r.Flags);
        }
    }
}
