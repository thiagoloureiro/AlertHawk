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
}
