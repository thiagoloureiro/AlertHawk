using AlertHawk.FinOps.Tests.Infrastructure;
using FinOpsToolSample.Controllers;
using FinOpsToolSample.Data;
using FinOpsToolSample.Data.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;

namespace AlertHawk.FinOps.Tests.Controllers;

public class HistoricalCostsControllerTests
{
    private static async Task<int> SeedRunAsync(FinOpsDbContext db)
    {
        var run = new AnalysisRun
        {
            SubscriptionId = "sub-x",
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
    public async Task GetHistoricalCostsByAnalysisRun_OrdersByCostDate()
    {
        await using var db = FinOpsDbContextFactory.Create();
        var runId = await SeedRunAsync(db);
        var d1 = new DateTime(2025, 1, 10, 0, 0, 0, DateTimeKind.Utc);
        var d2 = new DateTime(2025, 1, 20, 0, 0, 0, DateTimeKind.Utc);
        db.HistoricalCosts.AddRange(
            new HistoricalCost
            {
                AnalysisRunId = runId,
                SubscriptionId = "sub-x",
                CostDate = d2,
                CostType = "Total",
                Cost = 2,
                RecordedAt = DateTime.UtcNow
            },
            new HistoricalCost
            {
                AnalysisRunId = runId,
                SubscriptionId = "sub-x",
                CostDate = d1,
                CostType = "Total",
                Cost = 1,
                RecordedAt = DateTime.UtcNow
            });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var controller = new HistoricalCostsController(db, NullLogger<HistoricalCostsController>.Instance);
        var result = await controller.GetHistoricalCostsByAnalysisRun(runId);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var list = Assert.IsAssignableFrom<IEnumerable<HistoricalCost>>(ok.Value)!.ToList();
        Assert.Equal(2, list.Count);
        Assert.Equal(d1, list[0].CostDate);
        Assert.Equal(d2, list[1].CostDate);
    }

    [Fact]
    public async Task GetHistoricalCostsBySubscription_FiltersByDateRange()
    {
        await using var db = FinOpsDbContextFactory.Create();
        var runId = await SeedRunAsync(db);
        var inside = new DateTime(2025, 3, 15, 0, 0, 0, DateTimeKind.Utc);
        var before = new DateTime(2025, 3, 1, 0, 0, 0, DateTimeKind.Utc);
        db.HistoricalCosts.AddRange(
            new HistoricalCost
            {
                AnalysisRunId = runId,
                SubscriptionId = "sub-x",
                CostDate = before,
                CostType = "Total",
                Cost = 1,
                RecordedAt = DateTime.UtcNow
            },
            new HistoricalCost
            {
                AnalysisRunId = runId,
                SubscriptionId = "sub-x",
                CostDate = inside,
                CostType = "Total",
                Cost = 2,
                RecordedAt = DateTime.UtcNow
            });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var controller = new HistoricalCostsController(db, NullLogger<HistoricalCostsController>.Instance);
        var result = await controller.GetHistoricalCostsBySubscription(
            "sub-x",
            startDate: new DateTime(2025, 3, 10, 0, 0, 0, DateTimeKind.Utc),
            endDate: new DateTime(2025, 3, 20, 0, 0, 0, DateTimeKind.Utc));

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var list = Assert.IsAssignableFrom<IEnumerable<HistoricalCost>>(ok.Value)!.ToList();
        Assert.Single(list);
        Assert.Equal(inside, list[0].CostDate);
    }

    [Fact]
    public async Task GetDailyTotals_FiltersTotalCostType()
    {
        await using var db = FinOpsDbContextFactory.Create();
        var runId = await SeedRunAsync(db);
        var day = new DateTime(2025, 2, 1, 0, 0, 0, DateTimeKind.Utc);
        db.HistoricalCosts.AddRange(
            new HistoricalCost
            {
                AnalysisRunId = runId,
                SubscriptionId = "sub-x",
                CostDate = day,
                CostType = "Total",
                Cost = 50,
                Currency = "USD",
                RecordedAt = DateTime.UtcNow
            },
            new HistoricalCost
            {
                AnalysisRunId = runId,
                SubscriptionId = "sub-x",
                CostDate = day,
                CostType = "Service",
                Name = "Compute",
                Cost = 10,
                RecordedAt = DateTime.UtcNow
            });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var controller = new HistoricalCostsController(db, NullLogger<HistoricalCostsController>.Instance);
        var result = await controller.GetDailyTotals(runId);

        var ok = Assert.IsType<OkObjectResult>(result);
        var list = ok.Value as IEnumerable<dynamic>;
        Assert.NotNull(list);
        Assert.Single(list!);
    }

    [Fact]
    public async Task GetHistoricalCostsByResourceGroup_OptionalFilter()
    {
        await using var db = FinOpsDbContextFactory.Create();
        var runId = await SeedRunAsync(db);
        var day = new DateTime(2025, 4, 1, 0, 0, 0, DateTimeKind.Utc);
        db.HistoricalCosts.AddRange(
            new HistoricalCost
            {
                AnalysisRunId = runId,
                SubscriptionId = "sub-x",
                CostDate = day,
                CostType = "ResourceGroup",
                ResourceGroup = "rg-a",
                Cost = 5,
                RecordedAt = DateTime.UtcNow
            },
            new HistoricalCost
            {
                AnalysisRunId = runId,
                SubscriptionId = "sub-x",
                CostDate = day,
                CostType = "ResourceGroup",
                ResourceGroup = "rg-b",
                Cost = 7,
                RecordedAt = DateTime.UtcNow
            });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var controller = new HistoricalCostsController(db, NullLogger<HistoricalCostsController>.Instance);
        var filtered = await controller.GetHistoricalCostsByResourceGroup(runId, resourceGroup: "rg-a");
        var okFiltered = Assert.IsType<OkObjectResult>(filtered);
        var filteredList = okFiltered.Value as IEnumerable<dynamic>;
        Assert.NotNull(filteredList);
        Assert.Single(filteredList!);
    }

    [Fact]
    public async Task GetHistoricalCostsByService_OptionalFilter()
    {
        await using var db = FinOpsDbContextFactory.Create();
        var runId = await SeedRunAsync(db);
        var day = new DateTime(2025, 5, 1, 0, 0, 0, DateTimeKind.Utc);
        db.HistoricalCosts.AddRange(
            new HistoricalCost
            {
                AnalysisRunId = runId,
                SubscriptionId = "sub-x",
                CostDate = day,
                CostType = "Service",
                Name = "A",
                ResourceGroup = "rg",
                Cost = 1,
                RecordedAt = DateTime.UtcNow
            },
            new HistoricalCost
            {
                AnalysisRunId = runId,
                SubscriptionId = "sub-x",
                CostDate = day,
                CostType = "Service",
                Name = "B",
                ResourceGroup = "rg",
                Cost = 2,
                RecordedAt = DateTime.UtcNow
            });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var controller = new HistoricalCostsController(db, NullLogger<HistoricalCostsController>.Instance);
        var result = await controller.GetHistoricalCostsByService(runId, serviceName: "A");

        var ok = Assert.IsType<OkObjectResult>(result);
        var list = ok.Value as IEnumerable<dynamic>;
        Assert.NotNull(list);
        Assert.Single(list!);
    }

    [Fact]
    public async Task GetCostTrend_WhenFewerThanTwoPoints_ReturnsMessage()
    {
        await using var db = FinOpsDbContextFactory.Create();
        var runId = await SeedRunAsync(db);
        db.HistoricalCosts.Add(new HistoricalCost
        {
            AnalysisRunId = runId,
            SubscriptionId = "sub-x",
            CostDate = DateTime.UtcNow,
            CostType = "Total",
            Cost = 10,
            RecordedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var controller = new HistoricalCostsController(db, NullLogger<HistoricalCostsController>.Instance);
        var result = await controller.GetCostTrend(runId);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(ok.Value);
    }

    [Fact]
    public async Task GetCostTrend_WhenEnoughData_ReturnsTrendFields()
    {
        await using var db = FinOpsDbContextFactory.Create();
        var runId = await SeedRunAsync(db);
        db.HistoricalCosts.AddRange(
            new HistoricalCost
            {
                AnalysisRunId = runId,
                SubscriptionId = "sub-x",
                CostDate = new DateTime(2025, 6, 1, 0, 0, 0, DateTimeKind.Utc),
                CostType = "Total",
                Cost = 100,
                RecordedAt = DateTime.UtcNow
            },
            new HistoricalCost
            {
                AnalysisRunId = runId,
                SubscriptionId = "sub-x",
                CostDate = new DateTime(2025, 6, 10, 0, 0, 0, DateTimeKind.Utc),
                CostType = "Total",
                Cost = 80,
                RecordedAt = DateTime.UtcNow
            });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var controller = new HistoricalCostsController(db, NullLogger<HistoricalCostsController>.Instance);
        var result = await controller.GetCostTrend(runId);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(ok.Value);
    }
}
