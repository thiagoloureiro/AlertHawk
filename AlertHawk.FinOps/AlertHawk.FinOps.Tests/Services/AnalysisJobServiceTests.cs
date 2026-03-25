using FinOpsToolSample.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace AlertHawk.FinOps.Tests.Services;

public class AnalysisJobServiceTests
{
    [Fact]
    public async Task StartAnalysis_WhenOrchestrationSucceeds_CompletesWithResult()
    {
        var mockOrchestration = new Mock<IAnalysisOrchestrationService>();
        mockOrchestration
            .Setup(o => o.RunAnalysisForSingleSubscriptionAsync("sub-ok"))
            .ReturnsAsync(new SubscriptionAnalysisResult
            {
                Success = true,
                SubscriptionId = "sub-ok",
                SubscriptionName = "Test Sub",
                AnalysisRunId = 42,
                TotalMonthlyCost = 99,
                ResourcesAnalyzed = 7,
                Message = "done"
            });

        var services = new ServiceCollection();
        services.AddSingleton(mockOrchestration.Object);
        await using var provider = services.BuildServiceProvider();
        var scopeFactory = provider.GetRequiredService<IServiceScopeFactory>();

        var svc = new AnalysisJobService(scopeFactory, NullLogger<AnalysisJobService>.Instance);
        var jobId = svc.StartAnalysis("sub-ok");

        var status = await WaitForTerminalStatusAsync(svc, jobId, TimeSpan.FromSeconds(5));

        Assert.NotNull(status);
        Assert.Equal("completed", status.Status);
        Assert.True(status.Success);
        Assert.Equal("sub-ok", status.SubscriptionId);
        Assert.Equal("Test Sub", status.SubscriptionName);
        Assert.Equal(42, status.AnalysisRunId);
        Assert.Equal(99, status.TotalMonthlyCost);
        Assert.Equal(7, status.ResourcesAnalyzed);
    }

    [Fact]
    public async Task StartAnalysis_WhenOrchestrationThrows_MarksJobFailed()
    {
        var mockOrchestration = new Mock<IAnalysisOrchestrationService>();
        mockOrchestration
            .Setup(o => o.RunAnalysisForSingleSubscriptionAsync("sub-bad"))
            .ThrowsAsync(new InvalidOperationException("simulated failure"));

        var services = new ServiceCollection();
        services.AddSingleton(mockOrchestration.Object);
        await using var provider = services.BuildServiceProvider();
        var scopeFactory = provider.GetRequiredService<IServiceScopeFactory>();

        var svc = new AnalysisJobService(scopeFactory, NullLogger<AnalysisJobService>.Instance);
        var jobId = svc.StartAnalysis("sub-bad");

        var status = await WaitForTerminalStatusAsync(svc, jobId, TimeSpan.FromSeconds(5));

        Assert.NotNull(status);
        Assert.Equal("failed", status.Status);
        Assert.False(status.Success);
        Assert.Contains("simulated failure", status.Message ?? "");
    }

    [Fact]
    public void TryGetStatus_UnknownJobId_ReturnsFalse()
    {
        var services = new ServiceCollection();
        services.AddSingleton(Mock.Of<IAnalysisOrchestrationService>());
        using var provider = services.BuildServiceProvider();
        var svc = new AnalysisJobService(provider.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<AnalysisJobService>.Instance);

        var found = svc.TryGetStatus(Guid.NewGuid(), out var status);

        Assert.False(found);
        Assert.Null(status);
    }

    private static async Task<AnalysisJobStatusDto?> WaitForTerminalStatusAsync(
        AnalysisJobService svc,
        Guid jobId,
        TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            if (svc.TryGetStatus(jobId, out var st) &&
                st is { } s &&
                (s.Status == "completed" || s.Status == "failed"))
            {
                return s;
            }

            await Task.Delay(20);
        }

        return null;
    }
}
