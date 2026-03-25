using AlertHawk.FinOps.Tests.Infrastructure;
using FinOpsToolSample.Controllers;
using FinOpsToolSample.Data;
using FinOpsToolSample.Data.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;

namespace AlertHawk.FinOps.Tests.Controllers;

public class ResourcesControllerTests
{
    private static ResourcesController CreateController(FinOpsDbContext db)
    {
        return new ResourcesController(db, NullLogger<ResourcesController>.Instance)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
    }

    private static async Task<int> SeedRunWithResourcesAsync(FinOpsDbContext db)
    {
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
        db.ResourceAnalysis.AddRange(
            new ResourceAnalysis
            {
                AnalysisRunId = run.Id,
                ResourceType = "Z",
                ResourceName = "r1",
                ResourceGroup = "rg-a",
                Location = "eastus",
                RecordedAt = now
            },
            new ResourceAnalysis
            {
                AnalysisRunId = run.Id,
                ResourceType = "A",
                ResourceName = "r2",
                ResourceGroup = "rg-b",
                Location = "eastus",
                RecordedAt = now
            },
            new ResourceAnalysis
            {
                AnalysisRunId = run.Id,
                ResourceType = "A",
                ResourceName = "r3",
                ResourceGroup = "rg-b",
                Location = "eastus",
                Flags = "WARN:check",
                RecordedAt = now
            });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return run.Id;
    }

    [Fact]
    public async Task GetResourcesByAnalysisRun_PaginatesAndSetsHeaders()
    {
        await using var db = FinOpsDbContextFactory.Create();
        var runId = await SeedRunWithResourcesAsync(db);
        var controller = CreateController(db);
        var result = await controller.GetResourcesByAnalysisRun(runId, page: 1, pageSize: 2);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var list = Assert.IsAssignableFrom<IEnumerable<ResourceAnalysis>>(ok.Value)!.ToList();
        Assert.Equal(2, list.Count);
        Assert.Equal("A", list[0].ResourceType);
        Assert.Equal("A", list[1].ResourceType);

        Assert.Equal("3", controller.Response.Headers["X-Total-Count"].ToString());
        Assert.Equal("1", controller.Response.Headers["X-Page"].ToString());
        Assert.Equal("2", controller.Response.Headers["X-Page-Size"].ToString());
    }

    [Fact]
    public async Task GetResourcesByType_FiltersByType()
    {
        await using var db = FinOpsDbContextFactory.Create();
        var runId = await SeedRunWithResourcesAsync(db);
        var controller = CreateController(db);
        var result = await controller.GetResourcesByType(runId, "Z");

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var list = Assert.IsAssignableFrom<IEnumerable<ResourceAnalysis>>(ok.Value)!.ToList();
        Assert.Single(list);
        Assert.Equal("r1", list[0].ResourceName);
    }

    [Fact]
    public async Task GetResourcesByResourceGroup_FiltersByGroup()
    {
        await using var db = FinOpsDbContextFactory.Create();
        var runId = await SeedRunWithResourcesAsync(db);
        var controller = CreateController(db);
        var result = await controller.GetResourcesByResourceGroup(runId, "rg-a");

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var list = Assert.IsAssignableFrom<IEnumerable<ResourceAnalysis>>(ok.Value)!.ToList();
        Assert.Single(list);
    }

    [Fact]
    public async Task GetResourcesWithFlags_FiltersByContains()
    {
        await using var db = FinOpsDbContextFactory.Create();
        var runId = await SeedRunWithResourcesAsync(db);
        var controller = CreateController(db);
        var result = await controller.GetResourcesWithFlags(runId, flagContains: "WARN");

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var list = Assert.IsAssignableFrom<IEnumerable<ResourceAnalysis>>(ok.Value)!.ToList();
        Assert.Single(list);
    }

    [Fact]
    public async Task GetResourceGroupSummary_ReturnsOk()
    {
        await using var db = FinOpsDbContextFactory.Create();
        var runId = await SeedRunWithResourcesAsync(db);
        var controller = CreateController(db);
        var result = await controller.GetResourceGroupSummary(runId);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task SearchResources_EmptyTerm_ReturnsBadRequest()
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
        var result = await controller.SearchResources(run.Id, searchTerm: "  ");

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task SearchResources_FindsByName()
    {
        await using var db = FinOpsDbContextFactory.Create();
        var runId = await SeedRunWithResourcesAsync(db);
        var controller = CreateController(db);
        var result = await controller.SearchResources(runId, searchTerm: "r2");

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var list = Assert.IsAssignableFrom<IEnumerable<ResourceAnalysis>>(ok.Value)!.ToList();
        Assert.Single(list);
        Assert.Equal("r2", list[0].ResourceName);
    }
}
