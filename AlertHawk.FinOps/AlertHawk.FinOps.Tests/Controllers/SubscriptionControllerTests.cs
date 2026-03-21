using AlertHawk.FinOps.Tests.Infrastructure;
using FinOpsToolSample.Controllers;
using FinOpsToolSample.Data.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;

namespace AlertHawk.FinOps.Tests.Controllers;

public class SubscriptionControllerTests
{
    [Fact]
    public async Task GetSubscriptions_GroupsBySubscriptionId_UsesNameFromLatestRun()
    {
        await using var db = FinOpsDbContextFactory.Create();
        var older = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var newer = new DateTime(2025, 6, 1, 0, 0, 0, DateTimeKind.Utc);

        db.AnalysisRuns.AddRange(
            new AnalysisRun
            {
                SubscriptionId = "sub-1",
                SubscriptionName = "Old Name",
                RunDate = older,
                TotalMonthlyCost = 1,
                TotalResourcesAnalyzed = 0,
                AiModel = "m",
                ConversationId = "c",
                CreatedAt = older
            },
            new AnalysisRun
            {
                SubscriptionId = "sub-1",
                SubscriptionName = "Current Name",
                RunDate = newer,
                TotalMonthlyCost = 2,
                TotalResourcesAnalyzed = 0,
                AiModel = "m",
                ConversationId = "c",
                CreatedAt = newer
            },
            new AnalysisRun
            {
                SubscriptionId = "sub-2",
                SubscriptionName = "Other",
                RunDate = newer,
                TotalMonthlyCost = 3,
                TotalResourcesAnalyzed = 0,
                AiModel = "m",
                ConversationId = "c",
                CreatedAt = newer
            });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var controller = new SubscriptionController(db, NullLogger<SubscriptionController>.Instance);
        var result = await controller.GetSubscriptions();

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var list = Assert.IsAssignableFrom<IEnumerable<FinOpsToolSample.Models.SubscriptionSummary>>(ok.Value);
        var items = list.OrderBy(s => s.SubscriptionId).ToList();

        Assert.Equal(2, items.Count);
        Assert.Equal("sub-1", items[0].SubscriptionId);
        Assert.Equal("Current Name", items[0].SubscriptionName);
        Assert.Equal("sub-2", items[1].SubscriptionId);
    }
}
