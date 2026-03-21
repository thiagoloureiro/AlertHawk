using AlertHawk.FinOps.Tests.Infrastructure;
using FinOpsToolSample.Controllers;
using FinOpsToolSample.Data.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;

namespace AlertHawk.FinOps.Tests.Controllers;

public class DashboardControllerTests
{
    [Fact]
    public async Task GetDashboardSummary_NoRuns_ReturnsZeroTotalRunsMessage()
    {
        await using var db = FinOpsDbContextFactory.Create();
        var controller = new DashboardController(db, NullLogger<DashboardController>.Instance);

        var result = await controller.GetDashboardSummary();

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(ok.Value);
    }

    [Fact]
    public async Task GetResourceDistribution_NoRuns_ReturnsNotFound()
    {
        await using var db = FinOpsDbContextFactory.Create();
        var controller = new DashboardController(db, NullLogger<DashboardController>.Instance);

        var result = await controller.GetResourceDistribution();

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task GetDashboardSummary_WithRun_ReturnsAggregates()
    {
        await using var db = FinOpsDbContextFactory.Create();
        var run = new AnalysisRun
        {
            SubscriptionId = "s",
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

        db.ResourceAnalysis.Add(new ResourceAnalysis
        {
            AnalysisRunId = run.Id,
            ResourceType = "Microsoft.Compute/virtualMachines",
            ResourceName = "vm1",
            ResourceGroup = "rg",
            Location = "eastus",
            PropertiesJson = "{}",
            MetricsJson = "{}",
            Flags = "idle",
            RecordedAt = DateTime.UtcNow
        });
        db.CostDetails.Add(new CostDetail
        {
            AnalysisRunId = run.Id,
            CostType = "ResourceGroup",
            Name = "rg",
            ResourceGroup = "rg",
            Cost = 50,
            RecordedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var controller = new DashboardController(db, NullLogger<DashboardController>.Instance);
        var result = await controller.GetDashboardSummary();

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(ok.Value);
    }
}
