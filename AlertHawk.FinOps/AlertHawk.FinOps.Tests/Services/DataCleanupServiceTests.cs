using FinOpsToolSample.Data;
using FinOpsToolSample.Data.Entities;
using FinOpsToolSample.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace AlertHawk.FinOps.Tests.Services;

public class DataCleanupServiceTests
{
    private static FinOpsDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<FinOpsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new FinOpsDbContext(options);
    }

    [Fact]
    public async Task CleanupOldAnalysisRunsAsync_NoRuns_ReturnsSuccessWithMessage()
    {
        await using var dbContext = CreateInMemoryDbContext();
        var service = new DataCleanupService(dbContext, NullLogger<DataCleanupService>.Instance);

        var result = await service.CleanupOldAnalysisRunsAsync();

        Assert.True(result.Success);
        Assert.Equal(0, result.AnalysisRunsDeleted);
        Assert.Equal(0, result.AnalysisRunsKept);
        Assert.Contains("No old analysis runs found", result.Message);
    }

    [Fact]
    public async Task CleanupOldAnalysisRunsAsync_OneRunPerSubscription_KeepsAll()
    {
        await using var dbContext = CreateInMemoryDbContext();
        
        // Create one run per subscription
        dbContext.AnalysisRuns.AddRange(
            new AnalysisRun
            {
                SubscriptionId = "sub-1",
                SubscriptionName = "Subscription 1",
                RunDate = DateTime.UtcNow,
                TotalMonthlyCost = 100,
                TotalResourcesAnalyzed = 10,
                CreatedAt = DateTime.UtcNow
            },
            new AnalysisRun
            {
                SubscriptionId = "sub-2",
                SubscriptionName = "Subscription 2",
                RunDate = DateTime.UtcNow.AddHours(-1),
                TotalMonthlyCost = 200,
                TotalResourcesAnalyzed = 20,
                CreatedAt = DateTime.UtcNow.AddHours(-1)
            }
        );
        await dbContext.SaveChangesAsync();

        var service = new DataCleanupService(dbContext, NullLogger<DataCleanupService>.Instance);

        var result = await service.CleanupOldAnalysisRunsAsync();

        Assert.True(result.Success);
        Assert.Equal(0, result.AnalysisRunsDeleted);
        Assert.Equal(2, result.AnalysisRunsKept);
        Assert.Contains("No old analysis runs found", result.Message);
        Assert.Equal(2, await dbContext.AnalysisRuns.CountAsync());
    }

    [Fact]
    public async Task CleanupOldAnalysisRunsAsync_MultipleRunsPerSubscription_KeepsLatestOnly()
    {
        await using var dbContext = CreateInMemoryDbContext();
        
        var now = DateTime.UtcNow;
        
        // Create multiple runs for sub-1 (latest is most recent)
        dbContext.AnalysisRuns.AddRange(
            new AnalysisRun
            {
                SubscriptionId = "sub-1",
                SubscriptionName = "Subscription 1",
                RunDate = now.AddDays(-5),
                TotalMonthlyCost = 100,
                TotalResourcesAnalyzed = 10,
                CreatedAt = now.AddDays(-5)
            },
            new AnalysisRun
            {
                SubscriptionId = "sub-1",
                SubscriptionName = "Subscription 1",
                RunDate = now.AddDays(-3),
                TotalMonthlyCost = 120,
                TotalResourcesAnalyzed = 12,
                CreatedAt = now.AddDays(-3)
            },
            new AnalysisRun
            {
                SubscriptionId = "sub-1",
                SubscriptionName = "Subscription 1",
                RunDate = now,
                TotalMonthlyCost = 150,
                TotalResourcesAnalyzed = 15,
                CreatedAt = now
            },
            // Multiple runs for sub-2
            new AnalysisRun
            {
                SubscriptionId = "sub-2",
                SubscriptionName = "Subscription 2",
                RunDate = now.AddDays(-2),
                TotalMonthlyCost = 200,
                TotalResourcesAnalyzed = 20,
                CreatedAt = now.AddDays(-2)
            },
            new AnalysisRun
            {
                SubscriptionId = "sub-2",
                SubscriptionName = "Subscription 2",
                RunDate = now.AddDays(-1),
                TotalMonthlyCost = 220,
                TotalResourcesAnalyzed = 22,
                CreatedAt = now.AddDays(-1)
            }
        );
        await dbContext.SaveChangesAsync();

        var service = new DataCleanupService(dbContext, NullLogger<DataCleanupService>.Instance);

        var result = await service.CleanupOldAnalysisRunsAsync();

        Assert.True(result.Success);
        Assert.Equal(3, result.AnalysisRunsDeleted);
        Assert.Equal(2, result.AnalysisRunsKept);
        Assert.Equal(2, await dbContext.AnalysisRuns.CountAsync());
        
        // Verify the correct runs were kept
        var remainingRuns = await dbContext.AnalysisRuns.ToListAsync();
        Assert.All(remainingRuns, run => 
            Assert.True(run.RunDate >= now.AddDays(-1)));
    }

    [Fact]
    public async Task CleanupOldAnalysisRunsAsync_WithRelatedData_DeletesCascade()
    {
        await using var dbContext = CreateInMemoryDbContext();
        
        var now = DateTime.UtcNow;
        
        // Create old run with related data
        var oldRun = new AnalysisRun
        {
            SubscriptionId = "sub-1",
            SubscriptionName = "Subscription 1",
            RunDate = now.AddDays(-7),
            TotalMonthlyCost = 100,
            TotalResourcesAnalyzed = 2,
            CreatedAt = now.AddDays(-7)
        };
        
        dbContext.AnalysisRuns.Add(oldRun);
        await dbContext.SaveChangesAsync();

        // Add related data
        dbContext.CostDetails.Add(new CostDetail
        {
            AnalysisRunId = oldRun.Id,
            CostType = "Service",
            Name = "Virtual Machines",
            Cost = 50,
            RecordedAt = now.AddDays(-7)
        });

        dbContext.ResourceAnalysis.Add(new ResourceAnalysis
        {
            AnalysisRunId = oldRun.Id,
            ResourceType = "VirtualMachine",
            ResourceName = "vm-test",
            ResourceGroup = "rg-test",
            Location = "eastus",
            RecordedAt = now.AddDays(-7)
        });

        dbContext.AiRecommendations.Add(new AiRecommendation
        {
            AnalysisRunId = oldRun.Id,
            RecommendationText = "Test recommendation",
            MessageId = "msg-1",
            ConversationId = "conv-1",
            Model = "gpt-4",
            Timestamp = 123456,
            RecordedAt = now.AddDays(-7)
        });

        await dbContext.SaveChangesAsync();

        // Create latest run
        var latestRun = new AnalysisRun
        {
            SubscriptionId = "sub-1",
            SubscriptionName = "Subscription 1",
            RunDate = now,
            TotalMonthlyCost = 150,
            TotalResourcesAnalyzed = 3,
            CreatedAt = now
        };
        
        dbContext.AnalysisRuns.Add(latestRun);
        await dbContext.SaveChangesAsync();

        var service = new DataCleanupService(dbContext, NullLogger<DataCleanupService>.Instance);

        var result = await service.CleanupOldAnalysisRunsAsync();

        Assert.True(result.Success);
        Assert.Equal(1, result.AnalysisRunsDeleted);
        Assert.Equal(1, result.AnalysisRunsKept);
        
        // Verify cascade delete worked
        Assert.Equal(1, await dbContext.AnalysisRuns.CountAsync());
        Assert.Equal(0, await dbContext.CostDetails.CountAsync());
        Assert.Equal(0, await dbContext.ResourceAnalysis.CountAsync());
        Assert.Equal(0, await dbContext.AiRecommendations.CountAsync());
        
        // Verify the latest run is still there
        var remainingRun = await dbContext.AnalysisRuns.FirstAsync();
        Assert.Equal(latestRun.Id, remainingRun.Id);
    }

    [Fact]
    public async Task CleanupOldAnalysisRunsAsync_MultipleSubscriptionsWithOldData_KeepsLatestPerSub()
    {
        await using var dbContext = CreateInMemoryDbContext();
        
        var now = DateTime.UtcNow;
        
        // Sub-1: 4 runs
        for (int i = 0; i < 4; i++)
        {
            dbContext.AnalysisRuns.Add(new AnalysisRun
            {
                SubscriptionId = "sub-1",
                SubscriptionName = "Subscription 1",
                RunDate = now.AddDays(-i),
                TotalMonthlyCost = 100 + i,
                TotalResourcesAnalyzed = 10,
                CreatedAt = now.AddDays(-i)
            });
        }

        // Sub-2: 3 runs
        for (int i = 0; i < 3; i++)
        {
            dbContext.AnalysisRuns.Add(new AnalysisRun
            {
                SubscriptionId = "sub-2",
                SubscriptionName = "Subscription 2",
                RunDate = now.AddDays(-i * 2),
                TotalMonthlyCost = 200 + i,
                TotalResourcesAnalyzed = 20,
                CreatedAt = now.AddDays(-i * 2)
            });
        }

        // Sub-3: 2 runs
        for (int i = 0; i < 2; i++)
        {
            dbContext.AnalysisRuns.Add(new AnalysisRun
            {
                SubscriptionId = "sub-3",
                SubscriptionName = "Subscription 3",
                RunDate = now.AddDays(-i * 3),
                TotalMonthlyCost = 300 + i,
                TotalResourcesAnalyzed = 30,
                CreatedAt = now.AddDays(-i * 3)
            });
        }

        await dbContext.SaveChangesAsync();

        var service = new DataCleanupService(dbContext, NullLogger<DataCleanupService>.Instance);

        var result = await service.CleanupOldAnalysisRunsAsync();

        Assert.True(result.Success);
        Assert.Equal(6, result.AnalysisRunsDeleted); // 3 + 2 + 1
        Assert.Equal(3, result.AnalysisRunsKept); // one per subscription
        Assert.Equal(3, await dbContext.AnalysisRuns.CountAsync());
        
        // Verify each subscription has only its latest run
        var sub1Latest = await dbContext.AnalysisRuns
            .Where(ar => ar.SubscriptionId == "sub-1")
            .OrderByDescending(ar => ar.RunDate)
            .FirstAsync();
        Assert.Equal(now.Date, sub1Latest.RunDate.Date);

        var sub2Latest = await dbContext.AnalysisRuns
            .Where(ar => ar.SubscriptionId == "sub-2")
            .OrderByDescending(ar => ar.RunDate)
            .FirstAsync();
        Assert.Equal(now.Date, sub2Latest.RunDate.Date);

        var sub3Latest = await dbContext.AnalysisRuns
            .Where(ar => ar.SubscriptionId == "sub-3")
            .OrderByDescending(ar => ar.RunDate)
            .FirstAsync();
        Assert.Equal(now.Date, sub3Latest.RunDate.Date);
    }
}
