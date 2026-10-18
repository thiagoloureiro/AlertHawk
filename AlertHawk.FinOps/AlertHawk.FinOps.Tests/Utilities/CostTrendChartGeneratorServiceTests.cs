using AlertHawk.FinOps.Tests.Infrastructure;
using FinOpsToolSample.Data.Entities;
using FinOpsToolSample.Services;
using FinOpsToolSample.Utilities;

namespace AlertHawk.FinOps.Tests.Utilities;

public class CostTrendChartGeneratorServiceTests
{
    private static async Task<(FinOpsToolSample.Data.FinOpsDbContext db, CostTrendChartGenerator charts, string subId)> CreateWithDataAsync()
    {
        var db = FinOpsDbContextFactory.Create();
        var svc = new DatabaseService(db);
        var charts = new CostTrendChartGenerator(svc);
        const string subId = "sub-charts";

        var run = new AnalysisRun
        {
            SubscriptionId = subId,
            SubscriptionName = "Sub",
            RunDate = DateTime.UtcNow,
            TotalMonthlyCost = 200,
            TotalResourcesAnalyzed = 1,
            AiModel = "m",
            ConversationId = "c",
            CreatedAt = DateTime.UtcNow
        };
        db.AnalysisRuns.Add(run);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var now = DateTime.UtcNow.Date;
        db.HistoricalCosts.AddRange(
            new HistoricalCost
            {
                AnalysisRunId = run.Id,
                SubscriptionId = subId,
                CostDate = now.AddDays(-2),
                CostType = "Total",
                Cost = 10,
                RecordedAt = DateTime.UtcNow
            },
            new HistoricalCost
            {
                AnalysisRunId = run.Id,
                SubscriptionId = subId,
                CostDate = now.AddDays(-1),
                CostType = "Total",
                Cost = 20,
                RecordedAt = DateTime.UtcNow
            },
            new HistoricalCost
            {
                AnalysisRunId = run.Id,
                SubscriptionId = subId,
                CostDate = now.AddDays(-1),
                CostType = "ResourceGroup",
                Name = "rg-a",
                ResourceGroup = "rg-a",
                Cost = 15,
                RecordedAt = DateTime.UtcNow
            },
            new HistoricalCost
            {
                AnalysisRunId = run.Id,
                SubscriptionId = subId,
                CostDate = now.AddDays(-1),
                CostType = "Service",
                Name = "Compute",
                ResourceGroup = "rg-a",
                Cost = 15,
                RecordedAt = DateTime.UtcNow
            });

        var olderMonth = now.AddMonths(-1);
        db.HistoricalCosts.Add(new HistoricalCost
        {
            AnalysisRunId = run.Id,
            SubscriptionId = subId,
            CostDate = new DateTime(olderMonth.Year, olderMonth.Month, 5, 0, 0, 0, DateTimeKind.Utc),
            CostType = "Total",
            Cost = 50,
            RecordedAt = DateTime.UtcNow
        });

        db.CostDetails.Add(new CostDetail
        {
            AnalysisRunId = run.Id,
            CostType = "ResourceGroup",
            Name = "rg-a",
            ResourceGroup = "rg-a",
            Cost = 15,
            RecordedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        return (db, charts, subId);
    }

    [Fact]
    public async Task ShowDailyCostTrendAsync_WithData_Completes()
    {
        var (db, charts, subId) = await CreateWithDataAsync();
        await using (db)
        {
            await charts.ShowDailyCostTrendAsync(subId, days: 30);
        }
    }

    [Fact]
    public async Task ShowDailyCostTrendAsync_NoData_Completes()
    {
        await using var db = FinOpsDbContextFactory.Create();
        var charts = new CostTrendChartGenerator(new DatabaseService(db));

        await charts.ShowDailyCostTrendAsync("missing", days: 7);
    }

    [Fact]
    public async Task ShowMonthlyCostTrendAsync_WithData_Completes()
    {
        var (db, charts, subId) = await CreateWithDataAsync();
        await using (db)
        {
            await charts.ShowMonthlyCostTrendAsync(subId, months: 6);
        }
    }

    [Fact]
    public async Task ShowCostComparisonAsync_WithTwoMonths_Completes()
    {
        var (db, charts, subId) = await CreateWithDataAsync();
        await using (db)
        {
            await charts.ShowCostComparisonAsync(subId);
        }
    }

    [Fact]
    public async Task ShowCostComparisonAsync_InsufficientMonths_Completes()
    {
        await using var db = FinOpsDbContextFactory.Create();
        var charts = new CostTrendChartGenerator(new DatabaseService(db));

        await charts.ShowCostComparisonAsync("missing");
    }

    [Fact]
    public async Task ShowTopCostResourceGroupsAsync_WithData_Completes()
    {
        var (db, charts, subId) = await CreateWithDataAsync();
        await using (db)
        {
            await charts.ShowTopCostResourceGroupsAsync(subId, days: 30);
        }
    }

    [Fact]
    public async Task ShowTopCostResourceGroupsAsync_NoData_Completes()
    {
        await using var db = FinOpsDbContextFactory.Create();
        var charts = new CostTrendChartGenerator(new DatabaseService(db));

        await charts.ShowTopCostResourceGroupsAsync("missing");
    }

    [Fact]
    public async Task ShowResourceGroupCostBreakdownAsync_WithData_Completes()
    {
        var (db, charts, subId) = await CreateWithDataAsync();
        await using (db)
        {
            await charts.ShowResourceGroupCostBreakdownAsync(subId, "rg-a", days: 30);
        }
    }

    [Fact]
    public async Task ShowResourceGroupCostBreakdownAsync_NoData_Completes()
    {
        await using var db = FinOpsDbContextFactory.Create();
        var charts = new CostTrendChartGenerator(new DatabaseService(db));

        await charts.ShowResourceGroupCostBreakdownAsync("missing", "rg-x");
    }

    [Fact]
    public async Task ShowResourceGroupComparisonAsync_WithData_Completes()
    {
        var (db, charts, subId) = await CreateWithDataAsync();
        await using (db)
        {
            await charts.ShowResourceGroupComparisonAsync(subId, days: 30);
        }
    }

    [Fact]
    public async Task ShowResourceGroupComparisonAsync_NoData_Completes()
    {
        await using var db = FinOpsDbContextFactory.Create();
        var charts = new CostTrendChartGenerator(new DatabaseService(db));

        await charts.ShowResourceGroupComparisonAsync("missing");
    }
}
