using AlertHawk.FinOps.Tests.Infrastructure;
using FinOpsToolSample.Controllers;
using FinOpsToolSample.Data.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;

namespace AlertHawk.FinOps.Tests.Controllers;

public class AnalysisRunsControllerTests
{
    [Fact]
    public async Task GetAnalysisRuns_SetsPagingHeadersAndReturnsPage()
    {
        await using var db = FinOpsDbContextFactory.Create();
        for (var i = 0; i < 3; i++)
        {
            db.AnalysisRuns.Add(new AnalysisRun
            {
                SubscriptionId = "s",
                SubscriptionName = "Sub",
                RunDate = DateTime.UtcNow.AddMinutes(-i),
                TotalMonthlyCost = i,
                TotalResourcesAnalyzed = 0,
                AiModel = "m",
                ConversationId = "c",
                CreatedAt = DateTime.UtcNow
            });
        }

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var controller = new AnalysisRunsController(db, NullLogger<AnalysisRunsController>.Instance)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var result = await controller.GetAnalysisRuns(page: 1, pageSize: 2);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var runs = Assert.IsAssignableFrom<IEnumerable<AnalysisRun>>(ok.Value)!.ToList();
        Assert.Equal(2, runs.Count);

        Assert.True(controller.Response.Headers.TryGetValue("X-Total-Count", out var total));
        Assert.Equal("3", total.ToString());
    }
}
