using AlertHawk.FinOps.Tests.Infrastructure;
using FinOpsToolSample.Data.Entities;
using FinOpsToolSample.Models;
using FinOpsToolSample.Services;
using Microsoft.EntityFrameworkCore;

namespace AlertHawk.FinOps.Tests.Services;

public class DatabaseServiceTests
{
    [Fact]
    public async Task GetLatestAnalysisAsync_ReturnsMostRecentForSubscription()
    {
        await using var db = FinOpsDbContextFactory.Create();
        var svc = new DatabaseService(db);
        var subId = "subscription-1";

        db.AnalysisRuns.Add(new AnalysisRun
        {
            SubscriptionId = subId,
            SubscriptionName = "First",
            RunDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            TotalMonthlyCost = 1,
            TotalResourcesAnalyzed = 0,
            AiModel = "m",
            ConversationId = "c",
            CreatedAt = DateTime.UtcNow
        });
        db.AnalysisRuns.Add(new AnalysisRun
        {
            SubscriptionId = subId,
            SubscriptionName = "Latest",
            RunDate = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            TotalMonthlyCost = 2,
            TotalResourcesAnalyzed = 0,
            AiModel = "m",
            ConversationId = "c",
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var latest = await svc.GetLatestAnalysisAsync(subId);

        Assert.NotNull(latest);
        Assert.Equal("Latest", latest.SubscriptionName);
    }

    [Fact]
    public async Task SaveAnalysisRunAsync_PersistsRunCostDetailsAndResources()
    {
        await using var db = FinOpsDbContextFactory.Create();
        var svc = new DatabaseService(db);

        var data = new AzureResourceData
        {
            SubscriptionId = "s",
            SubscriptionName = "Sub",
            TotalMonthlyCost = 42,
            CostsByResourceGroup = new Dictionary<string, decimal> { ["rg1"] = 10 },
            CostsByService =
            [
                new ServiceCostDetail { ServiceName = "Compute", ResourceGroup = "rg1", Cost = 5 }
            ],
            Resources =
            [
                new ResourceInfo
                {
                    Type = "t",
                    Name = "r1",
                    ResourceGroup = "rg1",
                    Location = "eastus"
                }
            ]
        };

        var id = await svc.SaveAnalysisRunAsync(data, null, string.Empty);

        Assert.True(id > 0);
        var run = await db.AnalysisRuns.FindAsync([id], TestContext.Current.CancellationToken);
        Assert.NotNull(run);
        Assert.Equal(42, run.TotalMonthlyCost);
        Assert.Equal(2,
            await db.CostDetails.CountAsync(c => c.AnalysisRunId == id, TestContext.Current.CancellationToken));
        Assert.Single(db.ResourceAnalysis.Where(r => r.AnalysisRunId == id));
    }

    [Fact]
    public async Task SaveHistoricalCostsAsync_CreatesTotalRowsPerDay()
    {
        await using var db = FinOpsDbContextFactory.Create();
        var svc = new DatabaseService(db);
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

        var day = new DateTime(2025, 3, 10, 0, 0, 0, DateTimeKind.Utc);
        await svc.SaveHistoricalCostsAsync(run.Id,
        [
            new HistoricalCostData
            {
                SubscriptionId = "s",
                Date = day,
                Cost = 1,
                ResourceGroup = "rg",
                ServiceName = "S1"
            },
            new HistoricalCostData
            {
                SubscriptionId = "s",
                Date = day,
                Cost = 2,
                ResourceGroup = "rg",
                ServiceName = "S1"
            }
        ]);

        var totals = await db.HistoricalCosts
            .Where(h => h.AnalysisRunId == run.Id && h.CostType == "Total")
            .ToListAsync(TestContext.Current.CancellationToken);

        Assert.Single(totals);
        Assert.Equal(3, totals[0].Cost);
    }

    [Fact]
    public async Task GetMonthlySpendingAsync_GroupsTotalCostsByMonth()
    {
        await using var db = FinOpsDbContextFactory.Create();
        var svc = new DatabaseService(db);
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

        var now = DateTime.UtcNow;
        db.HistoricalCosts.Add(new HistoricalCost
        {
            AnalysisRunId = run.Id,
            SubscriptionId = "s",
            CostDate = new DateTime(now.Year, now.Month, 5, 0, 0, 0, DateTimeKind.Utc),
            CostType = "Total",
            Cost = 100,
            RecordedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var monthly = await svc.GetMonthlySpendingAsync("s", months: 12);

        var key = $"{now.Year}-{now.Month:D2}";
        Assert.True(monthly.ContainsKey(key));
        Assert.Equal(100, monthly[key]);
    }
}
