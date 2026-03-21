using AlertHawk.FinOps.Tests.Infrastructure;
using FinOpsToolSample.Controllers;
using FinOpsToolSample.Data.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;

namespace AlertHawk.FinOps.Tests.Controllers;

public class CostDetailsControllerTests
{
    [Fact]
    public async Task GetCostDetailsByAnalysisRun_OrdersByCostDescending()
    {
        await using var db = FinOpsDbContextFactory.Create();
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

        db.CostDetails.AddRange(
            new CostDetail
            {
                AnalysisRunId = run.Id,
                CostType = "Service",
                Name = "low",
                Cost = 10,
                RecordedAt = DateTime.UtcNow
            },
            new CostDetail
            {
                AnalysisRunId = run.Id,
                CostType = "Service",
                Name = "high",
                Cost = 99,
                RecordedAt = DateTime.UtcNow
            });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var controller = new CostDetailsController(db, NullLogger<CostDetailsController>.Instance);
        var result = await controller.GetCostDetailsByAnalysisRun(run.Id);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var list = Assert.IsAssignableFrom<IEnumerable<CostDetail>>(ok.Value)!.ToList();
        Assert.Equal(99m, list[0].Cost);
        Assert.Equal(10m, list[1].Cost);
    }
}
