using AlertHawk.FinOps.Tests.Infrastructure;
using FinOpsToolSample.Controllers;
using FinOpsToolSample.Data;
using FinOpsToolSample.Data.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;

namespace AlertHawk.FinOps.Tests.Controllers;

public class RecommendationsControllerTests
{
    private static RecommendationsController CreateController(FinOpsDbContext db)
    {
        return new RecommendationsController(db, NullLogger<RecommendationsController>.Instance)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
    }

    private static AiRecommendation SampleRecommendation(int analysisRunId, DateTime recordedAt) =>
        new()
        {
            AnalysisRunId = analysisRunId,
            RecommendationText = "line1\\nline2",
            Summary = "sum",
            MessageId = "msg-1",
            ConversationId = "conv",
            Model = "gpt",
            Timestamp = 0,
            RecordedAt = recordedAt
        };

    [Fact]
    public async Task GetRecommendationsByAnalysisRun_OrdersByRecordedAtDescending()
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

        var older = DateTime.UtcNow.AddDays(-1);
        var newer = DateTime.UtcNow;
        db.AiRecommendations.AddRange(
            SampleRecommendation(run.Id, older),
            SampleRecommendation(run.Id, newer));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var controller = CreateController(db);
        var result = await controller.GetRecommendationsByAnalysisRun(run.Id);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var list = Assert.IsAssignableFrom<IEnumerable<AiRecommendation>>(ok.Value)!.ToList();
        Assert.Equal(2, list.Count);
        Assert.True(list[0].RecordedAt >= list[1].RecordedAt);
    }

    [Fact]
    public async Task GetLatestRecommendation_WhenMissing_ReturnsNotFound()
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

        var controller = CreateController(db);
        var result = await controller.GetLatestRecommendation(run.Id);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task GetLatestRecommendation_WhenFound_ReturnsMostRecent()
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

        var older = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var newer = new DateTime(2025, 1, 10, 0, 0, 0, DateTimeKind.Utc);
        db.AiRecommendations.AddRange(
            SampleRecommendation(run.Id, older),
            SampleRecommendation(run.Id, newer));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var controller = CreateController(db);
        var result = await controller.GetLatestRecommendation(run.Id);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var rec = Assert.IsType<AiRecommendation>(ok.Value);
        Assert.Equal(newer, rec.RecordedAt);
    }

    [Fact]
    public async Task GetFormattedRecommendation_WhenFound_ReturnsPayload()
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

        db.AiRecommendations.Add(SampleRecommendation(run.Id, DateTime.UtcNow));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var id = db.AiRecommendations.First().Id;

        var controller = CreateController(db);
        var result = await controller.GetFormattedRecommendation(id);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.NotNull(ok.Value);
    }

    [Fact]
    public async Task GetAllRecommendations_SetsPaginationHeaders()
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

        db.AiRecommendations.Add(SampleRecommendation(run.Id, DateTime.UtcNow));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var controller = CreateController(db);
        var result = await controller.GetAllRecommendations(page: 1, pageSize: 10);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.NotNull(ok.Value);
        Assert.Equal("1", controller.Response.Headers["X-Total-Count"].ToString());
        Assert.Equal("1", controller.Response.Headers["X-Page"].ToString());
        Assert.Equal("10", controller.Response.Headers["X-Page-Size"].ToString());
    }
}
