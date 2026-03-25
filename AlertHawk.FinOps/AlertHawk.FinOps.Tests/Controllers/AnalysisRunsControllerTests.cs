using AlertHawk.FinOps.Tests.Infrastructure;
using FinOpsToolSample.Controllers;
using FinOpsToolSample.Data;
using FinOpsToolSample.Data.Entities;
using FinOpsToolSample.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;

namespace AlertHawk.FinOps.Tests.Controllers;

public class AnalysisRunsControllerTests
{
    private static AnalysisRunsController CreateController(FinOpsDbContext db)
    {
        return new AnalysisRunsController(db, NullLogger<AnalysisRunsController>.Instance)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
    }

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

        var controller = CreateController(db);
        var result = await controller.GetAnalysisRuns(page: 1, pageSize: 2);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var runs = Assert.IsAssignableFrom<IEnumerable<AnalysisRun>>(ok.Value)!.ToList();
        Assert.Equal(2, runs.Count);

        Assert.True(controller.Response.Headers.TryGetValue("X-Total-Count", out var total));
        Assert.Equal("3", total.ToString());
        Assert.Equal("1", controller.Response.Headers["X-Page"].ToString());
        Assert.Equal("2", controller.Response.Headers["X-Page-Size"].ToString());
    }

    [Fact]
    public async Task GetAnalysisRuns_OrderedByRunDateDescending()
    {
        await using var db = FinOpsDbContextFactory.Create();
        var old = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var recent = new DateTime(2025, 6, 1, 0, 0, 0, DateTimeKind.Utc);
        db.AnalysisRuns.AddRange(
            new AnalysisRun
            {
                SubscriptionId = "s",
                SubscriptionName = "Sub",
                RunDate = old,
                TotalMonthlyCost = 1,
                TotalResourcesAnalyzed = 0,
                AiModel = "m",
                ConversationId = "c",
                CreatedAt = DateTime.UtcNow
            },
            new AnalysisRun
            {
                SubscriptionId = "s",
                SubscriptionName = "Sub",
                RunDate = recent,
                TotalMonthlyCost = 2,
                TotalResourcesAnalyzed = 0,
                AiModel = "m",
                ConversationId = "c",
                CreatedAt = DateTime.UtcNow
            });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var controller = CreateController(db);
        var result = await controller.GetAnalysisRuns(page: 1, pageSize: 10);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var runs = Assert.IsAssignableFrom<IEnumerable<AnalysisRun>>(ok.Value)!.ToList();
        Assert.Equal(recent, runs[0].RunDate);
        Assert.Equal(old, runs[1].RunDate);
    }

    [Fact]
    public async Task GetAnalysisRun_WhenMissing_ReturnsNotFound()
    {
        await using var db = FinOpsDbContextFactory.Create();
        var controller = CreateController(db);

        var result = await controller.GetAnalysisRun(99999);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task GetAnalysisRun_WhenFound_ReturnsOkWithRelatedData()
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

        db.CostDetails.Add(new CostDetail
        {
            AnalysisRunId = run.Id,
            CostType = "Service",
            Name = "Compute",
            Cost = 10,
            RecordedAt = DateTime.UtcNow
        });
        db.ResourceAnalysis.Add(new ResourceAnalysis
        {
            AnalysisRunId = run.Id,
            ResourceType = "VM",
            ResourceName = "vm1",
            ResourceGroup = "rg",
            Location = "eastus",
            RecordedAt = DateTime.UtcNow
        });
        db.AiRecommendations.Add(new AiRecommendation
        {
            AnalysisRunId = run.Id,
            RecommendationText = "text",
            MessageId = "mid",
            ConversationId = "conv",
            Model = "gpt",
            Timestamp = 0,
            RecordedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var controller = CreateController(db);
        var result = await controller.GetAnalysisRun(run.Id);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var loaded = Assert.IsType<AnalysisRun>(ok.Value);
        Assert.Single(loaded.CostDetails);
        Assert.Single(loaded.Resources);
        Assert.Single(loaded.AiRecommendations);
    }

    [Fact]
    public async Task GetLatestAnalysisRun_WhenEmpty_ReturnsNotFound()
    {
        await using var db = FinOpsDbContextFactory.Create();
        var controller = CreateController(db);

        var result = await controller.GetLatestAnalysisRun();

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task GetLatestAnalysisRun_ReturnsRunWithLatestRunDate()
    {
        await using var db = FinOpsDbContextFactory.Create();
        var older = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var newer = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        db.AnalysisRuns.AddRange(
            new AnalysisRun
            {
                SubscriptionId = "s1",
                SubscriptionName = "Old",
                RunDate = older,
                TotalMonthlyCost = 1,
                TotalResourcesAnalyzed = 0,
                AiModel = "m",
                ConversationId = "c",
                CreatedAt = DateTime.UtcNow
            },
            new AnalysisRun
            {
                SubscriptionId = "s2",
                SubscriptionName = "New",
                RunDate = newer,
                TotalMonthlyCost = 2,
                TotalResourcesAnalyzed = 0,
                AiModel = "m",
                ConversationId = "c",
                CreatedAt = DateTime.UtcNow
            });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var controller = CreateController(db);
        var result = await controller.GetLatestAnalysisRun();

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var run = Assert.IsType<AnalysisRun>(ok.Value);
        Assert.Equal(newer, run.RunDate);
        Assert.Equal("New", run.SubscriptionName);
    }

    [Fact]
    public async Task GetLatestAnalysisRunsPerSubscription_ReturnsOneRowPerSubscription_WithDescriptionFromJoin()
    {
        await using var db = FinOpsDbContextFactory.Create();
        var t1 = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var t2 = new DateTime(2025, 2, 1, 0, 0, 0, DateTimeKind.Utc);
        db.AnalysisRuns.AddRange(
            new AnalysisRun
            {
                SubscriptionId = "sub-a",
                SubscriptionName = "Alpha",
                RunDate = t1,
                TotalMonthlyCost = 1,
                TotalResourcesAnalyzed = 0,
                AiModel = "m",
                ConversationId = "c",
                CreatedAt = DateTime.UtcNow
            },
            new AnalysisRun
            {
                SubscriptionId = "sub-a",
                SubscriptionName = "Alpha",
                RunDate = t2,
                TotalMonthlyCost = 2,
                TotalResourcesAnalyzed = 0,
                AiModel = "m",
                ConversationId = "c",
                CreatedAt = DateTime.UtcNow
            },
            new AnalysisRun
            {
                SubscriptionId = "sub-b",
                SubscriptionName = "Beta",
                RunDate = t1,
                TotalMonthlyCost = 3,
                TotalResourcesAnalyzed = 0,
                AiModel = "m",
                ConversationId = "c",
                CreatedAt = DateTime.UtcNow
            });
        db.Subscriptions.AddRange(
            new Subscription { SubscriptionId = "sub-a", Description = "Desc A", CreatedAt = DateTime.UtcNow },
            new Subscription { SubscriptionId = "sub-b", Description = "Desc B", CreatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var controller = CreateController(db);
        var result = await controller.GetLatestAnalysisRunsPerSubscription();

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var list = Assert.IsAssignableFrom<IEnumerable<AnalysisRunWithDescriptionDto>>(ok.Value)!.ToList();
        Assert.Equal(2, list.Count);
        Assert.Equal("Alpha", list[0].SubscriptionName);
        Assert.Equal("Desc A", list[0].Description);
        Assert.Equal(t2, list[0].RunDate);
        Assert.Equal("Beta", list[1].SubscriptionName);
        Assert.Equal("Desc B", list[1].Description);
    }

    [Fact]
    public async Task GetLatestAnalysisRunsPerSubscription_UsesEmptyDescription_WhenNoSubscriptionMetadata()
    {
        await using var db = FinOpsDbContextFactory.Create();
        db.AnalysisRuns.Add(new AnalysisRun
        {
            SubscriptionId = "orphan-sub",
            SubscriptionName = "Orphan",
            RunDate = new DateTime(2025, 3, 1, 0, 0, 0, DateTimeKind.Utc),
            TotalMonthlyCost = 0,
            TotalResourcesAnalyzed = 0,
            AiModel = "m",
            ConversationId = "c",
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var controller = CreateController(db);
        var result = await controller.GetLatestAnalysisRunsPerSubscription();

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var list = Assert.IsAssignableFrom<IEnumerable<AnalysisRunWithDescriptionDto>>(ok.Value)!.ToList();
        Assert.Single(list);
        Assert.Equal(string.Empty, list[0].Description);
    }

    [Fact]
    public async Task GetAnalysisRunsBySubscription_FiltersAndSetsHeaders()
    {
        await using var db = FinOpsDbContextFactory.Create();
        db.AnalysisRuns.AddRange(
            new AnalysisRun
            {
                SubscriptionId = "keep",
                SubscriptionName = "K",
                RunDate = DateTime.UtcNow,
                TotalMonthlyCost = 0,
                TotalResourcesAnalyzed = 0,
                AiModel = "m",
                ConversationId = "c",
                CreatedAt = DateTime.UtcNow
            },
            new AnalysisRun
            {
                SubscriptionId = "other",
                SubscriptionName = "O",
                RunDate = DateTime.UtcNow,
                TotalMonthlyCost = 0,
                TotalResourcesAnalyzed = 0,
                AiModel = "m",
                ConversationId = "c",
                CreatedAt = DateTime.UtcNow
            });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var controller = CreateController(db);
        var result = await controller.GetAnalysisRunsBySubscription("keep", page: 1, pageSize: 10);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var runs = Assert.IsAssignableFrom<IEnumerable<AnalysisRun>>(ok.Value)!.ToList();
        Assert.Single(runs);
        Assert.Equal("keep", runs[0].SubscriptionId);
        Assert.Equal("1", controller.Response.Headers["X-Total-Count"].ToString());
    }

    [Fact]
    public async Task DeleteAnalysisRun_WhenMissing_ReturnsNotFound()
    {
        await using var db = FinOpsDbContextFactory.Create();
        var controller = CreateController(db);

        var result = await controller.DeleteAnalysisRun(42);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task DeleteAnalysisRun_WhenFound_ReturnsNoContentAndRemoves()
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
        var id = run.Id;

        var controller = CreateController(db);
        var result = await controller.DeleteAnalysisRun(id);

        Assert.IsType<NoContentResult>(result);
        Assert.Null(await db.AnalysisRuns.FindAsync([id], TestContext.Current.CancellationToken));
    }
}
