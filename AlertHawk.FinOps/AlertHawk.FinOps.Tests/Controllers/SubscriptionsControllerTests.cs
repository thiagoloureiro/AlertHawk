using AlertHawk.FinOps.Tests.Infrastructure;
using FinOpsToolSample.Controllers;
using FinOpsToolSample.Data.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;

namespace AlertHawk.FinOps.Tests.Controllers;

public class SubscriptionsControllerTests
{
    [Fact]
    public async Task GetSubscriptions_ReturnsOrderedBySubscriptionId()
    {
        await using var db = FinOpsDbContextFactory.Create();
        db.Subscriptions.AddRange(
            new Subscription { SubscriptionId = "b-sub", Description = "B", CreatedAt = DateTime.UtcNow },
            new Subscription { SubscriptionId = "a-sub", Description = "A", CreatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var controller = new SubscriptionsController(db, NullLogger<SubscriptionsController>.Instance);
        var result = await controller.GetSubscriptions();

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var list = Assert.IsAssignableFrom<IEnumerable<Subscription>>(ok.Value)!.ToList();
        Assert.Equal(2, list.Count);
        Assert.Equal("a-sub", list[0].SubscriptionId);
        Assert.Equal("b-sub", list[1].SubscriptionId);
    }

    [Fact]
    public async Task GetSubscription_WhenMissing_ReturnsNotFound()
    {
        await using var db = FinOpsDbContextFactory.Create();
        var controller = new SubscriptionsController(db, NullLogger<SubscriptionsController>.Instance);

        var result = await controller.GetSubscription(999);

        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetSubscription_WhenFound_ReturnsOk()
    {
        await using var db = FinOpsDbContextFactory.Create();
        db.Subscriptions.Add(new Subscription { SubscriptionId = "x", Description = "d", CreatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var id = db.Subscriptions.First().Id;

        var controller = new SubscriptionsController(db, NullLogger<SubscriptionsController>.Instance);
        var result = await controller.GetSubscription(id);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var sub = Assert.IsType<Subscription>(ok.Value);
        Assert.Equal("x", sub.SubscriptionId);
    }

    [Fact]
    public async Task GetSubscriptionBySubscriptionId_WhenMissing_ReturnsNotFound()
    {
        await using var db = FinOpsDbContextFactory.Create();
        var controller = new SubscriptionsController(db, NullLogger<SubscriptionsController>.Instance);

        var result = await controller.GetSubscriptionBySubscriptionId("none");

        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task CreateOrUpdateSubscription_EmptySubscriptionId_ReturnsBadRequest()
    {
        await using var db = FinOpsDbContextFactory.Create();
        var controller = new SubscriptionsController(db, NullLogger<SubscriptionsController>.Instance);

        var result = await controller.CreateOrUpdateSubscription(new CreateSubscriptionDto { SubscriptionId = "  " });

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task CreateOrUpdateSubscription_CreatesThenUpdates()
    {
        await using var db = FinOpsDbContextFactory.Create();
        var controller = new SubscriptionsController(db, NullLogger<SubscriptionsController>.Instance);

        var create = await controller.CreateOrUpdateSubscription(new CreateSubscriptionDto
        {
            SubscriptionId = " sub-1 ",
            Description = "first"
        });
        var ok1 = Assert.IsType<OkObjectResult>(create.Result);
        var first = Assert.IsType<Subscription>(ok1.Value);
        Assert.Equal("sub-1", first.SubscriptionId);
        Assert.Equal("first", first.Description);

        var update = await controller.CreateOrUpdateSubscription(new CreateSubscriptionDto
        {
            SubscriptionId = "sub-1",
            Description = "second"
        });
        var ok2 = Assert.IsType<OkObjectResult>(update.Result);
        var second = Assert.IsType<Subscription>(ok2.Value);
        Assert.Equal("second", second.Description);
        Assert.Equal(first.Id, second.Id);
    }

    [Fact]
    public async Task UpdateSubscription_WhenMissing_ReturnsNotFound()
    {
        await using var db = FinOpsDbContextFactory.Create();
        var controller = new SubscriptionsController(db, NullLogger<SubscriptionsController>.Instance);

        var result = await controller.UpdateSubscription(1, new UpdateSubscriptionDto { Description = "x" });

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task UpdateSubscription_WhenFound_ReturnsOk()
    {
        await using var db = FinOpsDbContextFactory.Create();
        db.Subscriptions.Add(new Subscription { SubscriptionId = "u", Description = "old", CreatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var id = db.Subscriptions.First().Id;

        var controller = new SubscriptionsController(db, NullLogger<SubscriptionsController>.Instance);
        var result = await controller.UpdateSubscription(id, new UpdateSubscriptionDto { Description = "new" });

        var ok = Assert.IsType<OkObjectResult>(result);
        var sub = Assert.IsType<Subscription>(ok.Value);
        Assert.Equal("new", sub.Description);
        Assert.NotNull(sub.UpdatedAt);
    }

    [Fact]
    public async Task DeleteSubscription_WhenMissing_ReturnsNotFound()
    {
        await using var db = FinOpsDbContextFactory.Create();
        var controller = new SubscriptionsController(db, NullLogger<SubscriptionsController>.Instance);

        var result = await controller.DeleteSubscription(42);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task DeleteSubscription_WhenFound_ReturnsNoContent()
    {
        await using var db = FinOpsDbContextFactory.Create();
        db.Subscriptions.Add(new Subscription { SubscriptionId = "del", Description = "", CreatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var id = db.Subscriptions.First().Id;

        var controller = new SubscriptionsController(db, NullLogger<SubscriptionsController>.Instance);
        var result = await controller.DeleteSubscription(id);

        Assert.IsType<NoContentResult>(result);
        Assert.Empty(db.Subscriptions);
    }
}
