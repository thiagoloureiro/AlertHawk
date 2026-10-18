using AlertHawk.FinOps.Tests.Infrastructure;
using FinOpsToolSample.Data.Entities;
using FinOpsToolSample.Services;
using Microsoft.EntityFrameworkCore;

namespace AlertHawk.FinOps.Tests.Services;

public class DatabaseServiceQueryTests
{
    private static async Task<(FinOpsToolSample.Data.FinOpsDbContext db, DatabaseService svc, AnalysisRun run)> SeedAsync(
        string subscriptionId = "sub-1")
    {
        var db = FinOpsDbContextFactory.Create();
        var svc = new DatabaseService(db);
        var run = new AnalysisRun
        {
            SubscriptionId = subscriptionId,
            SubscriptionName = "Sub",
            RunDate = DateTime.UtcNow,
            TotalMonthlyCost = 100,
            TotalResourcesAnalyzed = 2,
            AiModel = "m",
            ConversationId = "c",
            CreatedAt = DateTime.UtcNow
        };
        db.AnalysisRuns.Add(run);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return (db, svc, run);
    }

    [Fact]
    public async Task GetAnalysisHistoryAsync_ReturnsRunsNewestFirst()
    {
        await using var db = FinOpsDbContextFactory.Create();
        var svc = new DatabaseService(db);
        db.AnalysisRuns.AddRange(
            new AnalysisRun
            {
                SubscriptionId = "s",
                SubscriptionName = "older",
                RunDate = DateTime.UtcNow.AddDays(-2),
                TotalMonthlyCost = 1,
                TotalResourcesAnalyzed = 0,
                AiModel = "m",
                ConversationId = "c",
                CreatedAt = DateTime.UtcNow
            },
            new AnalysisRun
            {
                SubscriptionId = "s",
                SubscriptionName = "newer",
                RunDate = DateTime.UtcNow,
                TotalMonthlyCost = 2,
                TotalResourcesAnalyzed = 0,
                AiModel = "m",
                ConversationId = "c",
                CreatedAt = DateTime.UtcNow
            });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var history = await svc.GetAnalysisHistoryAsync("s", take: 10);

        Assert.Equal(2, history.Count);
        Assert.Equal("newer", history[0].SubscriptionName);
    }

    [Fact]
    public async Task GetCostTrendAsync_FiltersByTypeAndSubscription()
    {
        var (db, svc, run) = await SeedAsync();
        await using (db)
        {
            db.CostDetails.AddRange(
                new CostDetail
                {
                    AnalysisRunId = run.Id,
                    CostType = "ResourceGroup",
                    Name = "rg-a",
                    ResourceGroup = "rg-a",
                    Cost = 10,
                    RecordedAt = DateTime.UtcNow
                },
                new CostDetail
                {
                    AnalysisRunId = run.Id,
                    CostType = "Service",
                    Name = "Compute",
                    ResourceGroup = "rg-a",
                    Cost = 5,
                    RecordedAt = DateTime.UtcNow
                });
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);

            var trends = await svc.GetCostTrendAsync("sub-1", "ResourceGroup", days: 30);

            Assert.Single(trends);
            Assert.Equal("rg-a", trends[0].Name);
        }
    }

    [Fact]
    public async Task GetResourceCountByTypeAsync_GroupsLatestRun()
    {
        var (db, svc, run) = await SeedAsync();
        await using (db)
        {
            db.ResourceAnalysis.AddRange(
                new ResourceAnalysis
                {
                    AnalysisRunId = run.Id,
                    ResourceType = "Microsoft.Compute/virtualMachines",
                    ResourceName = "vm1",
                    ResourceGroup = "rg",
                    Location = "eastus",
                    PropertiesJson = "{}",
                    MetricsJson = "{}",
                    RecordedAt = DateTime.UtcNow
                },
                new ResourceAnalysis
                {
                    AnalysisRunId = run.Id,
                    ResourceType = "Microsoft.Compute/virtualMachines",
                    ResourceName = "vm2",
                    ResourceGroup = "rg",
                    Location = "eastus",
                    PropertiesJson = "{}",
                    MetricsJson = "{}",
                    RecordedAt = DateTime.UtcNow
                },
                new ResourceAnalysis
                {
                    AnalysisRunId = run.Id,
                    ResourceType = "Microsoft.Storage/storageAccounts",
                    ResourceName = "sa1",
                    ResourceGroup = "rg",
                    Location = "eastus",
                    PropertiesJson = "{}",
                    MetricsJson = "{}",
                    RecordedAt = DateTime.UtcNow
                });
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);

            var counts = await svc.GetResourceCountByTypeAsync("sub-1");

            Assert.Equal(2, counts["Microsoft.Compute/virtualMachines"]);
            Assert.Equal(1, counts["Microsoft.Storage/storageAccounts"]);
        }
    }

    [Fact]
    public async Task GetResourceCountByTypeAsync_NoRuns_ReturnsEmpty()
    {
        await using var db = FinOpsDbContextFactory.Create();
        var svc = new DatabaseService(db);

        var counts = await svc.GetResourceCountByTypeAsync("missing");

        Assert.Empty(counts);
    }

    [Fact]
    public async Task GetFlaggedResourcesAsync_ReturnsOnlyFlagged()
    {
        var (db, svc, run) = await SeedAsync();
        await using (db)
        {
            db.ResourceAnalysis.AddRange(
                new ResourceAnalysis
                {
                    AnalysisRunId = run.Id,
                    ResourceType = "vm",
                    ResourceName = "ok",
                    ResourceGroup = "rg",
                    Location = "eastus",
                    PropertiesJson = "{}",
                    MetricsJson = "{}",
                    Flags = "",
                    RecordedAt = DateTime.UtcNow
                },
                new ResourceAnalysis
                {
                    AnalysisRunId = run.Id,
                    ResourceType = "vm",
                    ResourceName = "idle",
                    ResourceGroup = "rg",
                    Location = "eastus",
                    PropertiesJson = "{}",
                    MetricsJson = "{}",
                    Flags = "idle",
                    RecordedAt = DateTime.UtcNow
                });
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);

            var flagged = await svc.GetFlaggedResourcesAsync("sub-1");

            Assert.Single(flagged);
            Assert.Equal("idle", flagged[0].ResourceName);
        }
    }

    [Fact]
    public async Task GetDailyCostTrendAsync_ReturnsTotalRowsInRange()
    {
        var (db, svc, run) = await SeedAsync();
        await using (db)
        {
            db.HistoricalCosts.AddRange(
                new HistoricalCost
                {
                    AnalysisRunId = run.Id,
                    SubscriptionId = "sub-1",
                    CostDate = DateTime.UtcNow.Date.AddDays(-1),
                    CostType = "Total",
                    Cost = 11,
                    RecordedAt = DateTime.UtcNow
                },
                new HistoricalCost
                {
                    AnalysisRunId = run.Id,
                    SubscriptionId = "sub-1",
                    CostDate = DateTime.UtcNow.Date.AddDays(-1),
                    CostType = "Service",
                    Name = "Compute",
                    Cost = 11,
                    RecordedAt = DateTime.UtcNow
                });
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);

            var daily = await svc.GetDailyCostTrendAsync("sub-1", days: 7);

            Assert.Single(daily);
            Assert.Equal(11, daily[0].Cost);
        }
    }

    [Fact]
    public async Task GetCostByResourceGroupTrendAsync_FiltersByName()
    {
        var (db, svc, run) = await SeedAsync();
        await using (db)
        {
            db.HistoricalCosts.Add(new HistoricalCost
            {
                AnalysisRunId = run.Id,
                SubscriptionId = "sub-1",
                CostDate = DateTime.UtcNow.Date,
                CostType = "ResourceGroup",
                Name = "rg-prod",
                ResourceGroup = "rg-prod",
                Cost = 40,
                RecordedAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);

            var trend = await svc.GetCostByResourceGroupTrendAsync("sub-1", "rg-prod", days: 30);

            Assert.Single(trend);
            Assert.Equal(40, trend[0].Cost);
        }
    }

    [Fact]
    public async Task GetCostByServiceTrendAsync_FiltersByServiceName()
    {
        var (db, svc, run) = await SeedAsync();
        await using (db)
        {
            db.HistoricalCosts.Add(new HistoricalCost
            {
                AnalysisRunId = run.Id,
                SubscriptionId = "sub-1",
                CostDate = DateTime.UtcNow.Date,
                CostType = "Service",
                Name = "Storage",
                ResourceGroup = "rg",
                Cost = 12,
                RecordedAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);

            var trend = await svc.GetCostByServiceTrendAsync("sub-1", "Storage", days: 30);

            Assert.Single(trend);
            Assert.Equal(12, trend[0].Cost);
        }
    }

    [Fact]
    public async Task GetMonthlySpendingByResourceGroupAsync_GroupsByMonth()
    {
        var (db, svc, run) = await SeedAsync();
        await using (db)
        {
            var now = DateTime.UtcNow;
            db.HistoricalCosts.Add(new HistoricalCost
            {
                AnalysisRunId = run.Id,
                SubscriptionId = "sub-1",
                CostDate = new DateTime(now.Year, now.Month, 3, 0, 0, 0, DateTimeKind.Utc),
                CostType = "Service",
                Name = "Compute",
                ResourceGroup = "rg-a",
                Cost = 25,
                RecordedAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);

            var monthly = await svc.GetMonthlySpendingByResourceGroupAsync("sub-1", "rg-a", months: 12);

            var key = $"{now.Year}-{now.Month:D2}";
            Assert.Equal(25, monthly[key]);
        }
    }

    [Fact]
    public async Task GetServiceCostsForResourceGroupAsync_ReturnsServiceRows()
    {
        var (db, svc, run) = await SeedAsync();
        await using (db)
        {
            db.HistoricalCosts.Add(new HistoricalCost
            {
                AnalysisRunId = run.Id,
                SubscriptionId = "sub-1",
                CostDate = DateTime.UtcNow.Date,
                CostType = "Service",
                Name = "SQL",
                ResourceGroup = "rg-db",
                Cost = 9,
                RecordedAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);

            var rows = await svc.GetServiceCostsForResourceGroupAsync("sub-1", "rg-db", days: 30);

            Assert.Single(rows);
            Assert.Equal("SQL", rows[0].Name);
        }
    }

    [Fact]
    public async Task GetTopServicesInResourceGroupAsync_AggregatesAndOrders()
    {
        var (db, svc, run) = await SeedAsync();
        await using (db)
        {
            db.HistoricalCosts.AddRange(
                new HistoricalCost
                {
                    AnalysisRunId = run.Id,
                    SubscriptionId = "sub-1",
                    CostDate = DateTime.UtcNow.Date,
                    CostType = "Service",
                    Name = "A",
                    ResourceGroup = "rg",
                    Cost = 10,
                    RecordedAt = DateTime.UtcNow
                },
                new HistoricalCost
                {
                    AnalysisRunId = run.Id,
                    SubscriptionId = "sub-1",
                    CostDate = DateTime.UtcNow.Date,
                    CostType = "Service",
                    Name = "B",
                    ResourceGroup = "rg",
                    Cost = 30,
                    RecordedAt = DateTime.UtcNow
                });
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);

            var tops = await svc.GetTopServicesInResourceGroupAsync("sub-1", "rg", days: 30);

            Assert.Equal(2, tops.Count);
            Assert.Equal(30, tops["B"]);
            Assert.Equal(10, tops["A"]);
        }
    }
}
