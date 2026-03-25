using AlertHawk.FinOps.Tests.Infrastructure;
using FinOpsToolSample.Controllers;
using FinOpsToolSample.Data;
using FinOpsToolSample.Data.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;

namespace AlertHawk.FinOps.Tests.Controllers;

public class CostDetailsControllerTests
{
    private static async Task<int> SeedAnalysisRunAsync(FinOpsDbContext db)
    {
        var run = new AnalysisRun
        {
            SubscriptionId = "s",
            SubscriptionName = "Sub",
            RunDate = DateTime.UtcNow,
            TotalMonthlyCost = 0,
            TotalResourcesAnalyzed = 0,
            AiModel = "m",
            ConversationId = "c",
            CreatedAt = DateTime.UtcNow
        };
        db.AnalysisRuns.Add(run);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return run.Id;
    }

    [Fact]
    public async Task GetCostDetailsByAnalysisRun_OrdersByCostDescending()
    {
        await using var db = FinOpsDbContextFactory.Create();
        var runId = await SeedAnalysisRunAsync(db);
        db.CostDetails.AddRange(
            new CostDetail
            {
                AnalysisRunId = runId,
                CostType = "Service",
                Name = "low",
                Cost = 10,
                RecordedAt = DateTime.UtcNow
            },
            new CostDetail
            {
                AnalysisRunId = runId,
                CostType = "Service",
                Name = "high",
                Cost = 99,
                RecordedAt = DateTime.UtcNow
            });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var controller = new CostDetailsController(db, NullLogger<CostDetailsController>.Instance);
        var result = await controller.GetCostDetailsByAnalysisRun(runId);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var list = Assert.IsAssignableFrom<IEnumerable<CostDetail>>(ok.Value)!.ToList();
        Assert.Equal(99m, list[0].Cost);
        Assert.Equal(10m, list[1].Cost);
    }

    [Fact]
    public async Task GetCostDetailsByAnalysisRun_ExcludesOtherRuns()
    {
        await using var db = FinOpsDbContextFactory.Create();
        var runA = await SeedAnalysisRunAsync(db);
        var runB = await SeedAnalysisRunAsync(db);
        db.CostDetails.Add(new CostDetail
        {
            AnalysisRunId = runA,
            CostType = "Service",
            Name = "only-a",
            Cost = 5,
            RecordedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var controller = new CostDetailsController(db, NullLogger<CostDetailsController>.Instance);
        var result = await controller.GetCostDetailsByAnalysisRun(runB);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var list = Assert.IsAssignableFrom<IEnumerable<CostDetail>>(ok.Value)!.ToList();
        Assert.Empty(list);
    }

    [Fact]
    public async Task GetCostDetailsByType_FiltersAndOrdersByCostDescending()
    {
        await using var db = FinOpsDbContextFactory.Create();
        var runId = await SeedAnalysisRunAsync(db);
        db.CostDetails.AddRange(
            new CostDetail
            {
                AnalysisRunId = runId,
                CostType = "Service",
                Name = "svc-a",
                Cost = 20,
                RecordedAt = DateTime.UtcNow
            },
            new CostDetail
            {
                AnalysisRunId = runId,
                CostType = "ResourceGroup",
                Name = "rg",
                Cost = 999,
                RecordedAt = DateTime.UtcNow
            },
            new CostDetail
            {
                AnalysisRunId = runId,
                CostType = "Service",
                Name = "svc-b",
                Cost = 50,
                RecordedAt = DateTime.UtcNow
            });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var controller = new CostDetailsController(db, NullLogger<CostDetailsController>.Instance);
        var result = await controller.GetCostDetailsByType(runId, "Service");

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var list = Assert.IsAssignableFrom<IEnumerable<CostDetail>>(ok.Value)!.ToList();
        Assert.Equal(2, list.Count);
        Assert.Equal(50m, list[0].Cost);
        Assert.Equal(20m, list[1].Cost);
    }

    [Fact]
    public async Task GetTopCostDetails_ReturnsAtMostCountOrderedByCost()
    {
        await using var db = FinOpsDbContextFactory.Create();
        var runId = await SeedAnalysisRunAsync(db);
        db.CostDetails.AddRange(
            new CostDetail
            {
                AnalysisRunId = runId,
                CostType = "Service",
                Name = "c",
                Cost = 30,
                RecordedAt = DateTime.UtcNow
            },
            new CostDetail
            {
                AnalysisRunId = runId,
                CostType = "Service",
                Name = "b",
                Cost = 50,
                RecordedAt = DateTime.UtcNow
            },
            new CostDetail
            {
                AnalysisRunId = runId,
                CostType = "Service",
                Name = "a",
                Cost = 10,
                RecordedAt = DateTime.UtcNow
            });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var controller = new CostDetailsController(db, NullLogger<CostDetailsController>.Instance);
        var result = await controller.GetTopCostDetails(runId, count: 2);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var list = Assert.IsAssignableFrom<IEnumerable<CostDetail>>(ok.Value)!.ToList();
        Assert.Equal(2, list.Count);
        Assert.Equal(50m, list[0].Cost);
        Assert.Equal(30m, list[1].Cost);
    }

    [Fact]
    public async Task GetCostSummaryByResourceGroup_AggregatesByNameDescendingTotal()
    {
        await using var db = FinOpsDbContextFactory.Create();
        var runId = await SeedAnalysisRunAsync(db);
        db.CostDetails.AddRange(
            new CostDetail
            {
                AnalysisRunId = runId,
                CostType = "ResourceGroup",
                Name = "rg-small",
                Cost = 10,
                RecordedAt = DateTime.UtcNow
            },
            new CostDetail
            {
                AnalysisRunId = runId,
                CostType = "ResourceGroup",
                Name = "rg-big",
                Cost = 60,
                RecordedAt = DateTime.UtcNow
            },
            new CostDetail
            {
                AnalysisRunId = runId,
                CostType = "ResourceGroup",
                Name = "rg-big",
                Cost = 50,
                RecordedAt = DateTime.UtcNow
            },
            new CostDetail
            {
                AnalysisRunId = runId,
                CostType = "Service",
                Name = "ignore",
                Cost = 1000,
                RecordedAt = DateTime.UtcNow
            });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var controller = new CostDetailsController(db, NullLogger<CostDetailsController>.Instance);
        var result = await controller.GetCostSummaryByResourceGroup(runId);

        var ok = Assert.IsType<OkObjectResult>(result);
        var rows = ToSummaryRows(ok.Value);
        Assert.Equal(2, rows.Count);
        Assert.Equal("rg-big", rows[0].Key);
        Assert.Equal(110m, rows[0].TotalCost);
        Assert.Equal(2, rows[0].ItemCount);
        Assert.Equal("rg-small", rows[1].Key);
        Assert.Equal(10m, rows[1].TotalCost);
    }

    [Fact]
    public async Task GetCostSummaryByService_AggregatesByNameDescendingTotal()
    {
        await using var db = FinOpsDbContextFactory.Create();
        var runId = await SeedAnalysisRunAsync(db);
        db.CostDetails.AddRange(
            new CostDetail
            {
                AnalysisRunId = runId,
                CostType = "Service",
                Name = "Storage",
                Cost = 200,
                RecordedAt = DateTime.UtcNow
            },
            new CostDetail
            {
                AnalysisRunId = runId,
                CostType = "Service",
                Name = "Compute",
                Cost = 40,
                RecordedAt = DateTime.UtcNow
            },
            new CostDetail
            {
                AnalysisRunId = runId,
                CostType = "Service",
                Name = "Compute",
                Cost = 10,
                RecordedAt = DateTime.UtcNow
            },
            new CostDetail
            {
                AnalysisRunId = runId,
                CostType = "ResourceGroup",
                Name = "rg",
                Cost = 999,
                RecordedAt = DateTime.UtcNow
            });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var controller = new CostDetailsController(db, NullLogger<CostDetailsController>.Instance);
        var result = await controller.GetCostSummaryByService(runId);

        var ok = Assert.IsType<OkObjectResult>(result);
        var rows = ToServiceSummaryRows(ok.Value);
        Assert.Equal(2, rows.Count);
        Assert.Equal("Storage", rows[0].Service);
        Assert.Equal(200m, rows[0].TotalCost);
        Assert.Equal("Compute", rows[1].Service);
        Assert.Equal(50m, rows[1].TotalCost);
        Assert.Equal(2, rows[1].ItemCount);
    }

    private static List<(string Key, decimal TotalCost, int ItemCount)> ToSummaryRows(object? value)
    {
        Assert.NotNull(value);
        var list = new List<(string, decimal, int)>();
        foreach (var item in (System.Collections.IEnumerable)value)
        {
            Assert.NotNull(item);
            var t = item.GetType();
            var rg = t.GetProperty("ResourceGroup")!.GetValue(item)!.ToString()!;
            var total = (decimal)t.GetProperty("TotalCost")!.GetValue(item)!;
            var count = (int)t.GetProperty("ItemCount")!.GetValue(item)!;
            list.Add((rg, total, count));
        }

        return list;
    }

    private static List<(string Service, decimal TotalCost, int ItemCount)> ToServiceSummaryRows(object? value)
    {
        Assert.NotNull(value);
        var list = new List<(string, decimal, int)>();
        foreach (var item in (System.Collections.IEnumerable)value)
        {
            Assert.NotNull(item);
            var t = item.GetType();
            var svc = t.GetProperty("Service")!.GetValue(item)!.ToString()!;
            var total = (decimal)t.GetProperty("TotalCost")!.GetValue(item)!;
            var count = (int)t.GetProperty("ItemCount")!.GetValue(item)!;
            list.Add((svc, total, count));
        }

        return list;
    }
}
