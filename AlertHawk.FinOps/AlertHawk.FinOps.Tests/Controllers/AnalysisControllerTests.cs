using FinOpsToolSample.Controllers;
using FinOpsToolSample.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace AlertHawk.FinOps.Tests.Controllers;

public class AnalysisControllerTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task StartAnalysis_InvalidSubscriptionId_ReturnsBadRequest(string? subscriptionId)
    {
        var orchestration = new Mock<IAnalysisOrchestrationService>(MockBehavior.Strict);
        var jobs = new Mock<IAnalysisJobService>(MockBehavior.Strict);
        var cleanup = new Mock<IDataCleanupService>(MockBehavior.Strict);
        var config = new ConfigurationBuilder().AddInMemoryCollection().Build();
        var controller = new AnalysisController(
            NullLogger<AnalysisController>.Instance,
            orchestration.Object,
            jobs.Object,
            cleanup.Object,
            config);

        var result = await controller.StartAnalysis(subscriptionId!);

        Assert.IsType<BadRequestObjectResult>(result);
        orchestration.Verify(
            o => o.RunAnalysisForSingleSubscriptionAsync(It.IsAny<string>()),
            Times.Never);
        jobs.Verify(j => j.StartAnalysis(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task StartAnalysis_ValidId_ReturnsOkWithPayload()
    {
        var orchestration = new Mock<IAnalysisOrchestrationService>();
        var jobs = new Mock<IAnalysisJobService>(MockBehavior.Strict);
        var cleanup = new Mock<IDataCleanupService>(MockBehavior.Strict);
        orchestration
            .Setup(o => o.RunAnalysisForSingleSubscriptionAsync("sub-a"))
            .ReturnsAsync(new SubscriptionAnalysisResult
            {
                Success = true,
                SubscriptionId = "sub-a",
                SubscriptionName = "Name",
                AnalysisRunId = 5,
                TotalMonthlyCost = 12.5m,
                ResourcesAnalyzed = 3,
                Message = "done"
            });

        var config = new ConfigurationBuilder().AddInMemoryCollection().Build();
        var controller = new AnalysisController(
            NullLogger<AnalysisController>.Instance,
            orchestration.Object,
            jobs.Object,
            cleanup.Object,
            config);

        var result = await controller.StartAnalysis("sub-a");

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(ok.Value);
    }

    [Fact]
    public async Task StartAnalysis_WhenOrchestrationThrows_Returns500()
    {
        var orchestration = new Mock<IAnalysisOrchestrationService>();
        var jobs = new Mock<IAnalysisJobService>(MockBehavior.Strict);
        var cleanup = new Mock<IDataCleanupService>(MockBehavior.Strict);
        orchestration
            .Setup(o => o.RunAnalysisForSingleSubscriptionAsync("sub-x"))
            .ThrowsAsync(new InvalidOperationException("boom"));

        var config = new ConfigurationBuilder().AddInMemoryCollection().Build();
        var controller = new AnalysisController(
            NullLogger<AnalysisController>.Instance,
            orchestration.Object,
            jobs.Object,
            cleanup.Object,
            config);

        var result = await controller.StartAnalysis("sub-x");

        var status = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, status.StatusCode);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void StartAnalysisAsync_InvalidSubscriptionId_ReturnsBadRequest(string? subscriptionId)
    {
        var orchestration = new Mock<IAnalysisOrchestrationService>(MockBehavior.Strict);
        var jobs = new Mock<IAnalysisJobService>(MockBehavior.Strict);
        var cleanup = new Mock<IDataCleanupService>(MockBehavior.Strict);
        var controller = new AnalysisController(
            NullLogger<AnalysisController>.Instance,
            orchestration.Object,
            jobs.Object,
            cleanup.Object,
            new ConfigurationBuilder().Build());

        var result = controller.StartAnalysisAsync(subscriptionId!);

        Assert.IsType<BadRequestObjectResult>(result);
        jobs.Verify(j => j.StartAnalysis(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public void StartAnalysisAsync_ValidId_ReturnsAcceptedWithJobId()
    {
        var jobId = Guid.Parse("f1111111-1111-1111-1111-111111111111");
        var jobs = new Mock<IAnalysisJobService>();
        jobs.Setup(j => j.StartAnalysis("sub-z")).Returns(jobId);

        var controller = new AnalysisController(
            NullLogger<AnalysisController>.Instance,
            new Mock<IAnalysisOrchestrationService>(MockBehavior.Strict).Object,
            jobs.Object,
            new Mock<IDataCleanupService>(MockBehavior.Strict).Object,
            new ConfigurationBuilder().Build());

        var result = controller.StartAnalysisAsync("sub-z");

        var accepted = Assert.IsType<AcceptedAtActionResult>(result);
        Assert.Equal(nameof(AnalysisController.GetAnalysisJobStatus), accepted.ActionName);
        jobs.Verify(j => j.StartAnalysis("sub-z"), Times.Once);
    }

    [Fact]
    public void GetAnalysisJobStatus_UnknownJob_ReturnsNotFound()
    {
        var jobs = new Mock<IAnalysisJobService>();
        jobs.Setup(j => j.TryGetStatus(It.IsAny<Guid>(), out It.Ref<AnalysisJobStatusDto?>.IsAny))
            .Returns(false);

        var controller = new AnalysisController(
            NullLogger<AnalysisController>.Instance,
            new Mock<IAnalysisOrchestrationService>(MockBehavior.Strict).Object,
            jobs.Object,
            new Mock<IDataCleanupService>(MockBehavior.Strict).Object,
            new ConfigurationBuilder().Build());

        var result = controller.GetAnalysisJobStatus(Guid.NewGuid());

        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public void GetAnalysisJobStatus_KnownJob_ReturnsOk()
    {
        var jobId = Guid.NewGuid();
        var dto = new AnalysisJobStatusDto
        {
            JobId = jobId,
            SubscriptionId = "sub",
            Status = "running",
            CreatedAt = DateTimeOffset.UtcNow
        };

        var jobs = new StubAnalysisJobService();
        jobs.StatusByJobId[jobId] = dto;

        var controller = new AnalysisController(
            NullLogger<AnalysisController>.Instance,
            new Mock<IAnalysisOrchestrationService>(MockBehavior.Strict).Object,
            jobs,
            new Mock<IDataCleanupService>(MockBehavior.Strict).Object,
            new ConfigurationBuilder().Build());

        var result = controller.GetAnalysisJobStatus(jobId);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var body = Assert.IsType<AnalysisJobStatusDto>(ok.Value);
        Assert.Equal("running", body.Status);
    }

    [Fact]
    public async Task CleanupOldAnalysisRuns_Success_ReturnsOk()
    {
        var cleanup = new Mock<IDataCleanupService>();
        cleanup
            .Setup(c => c.CleanupOldAnalysisRunsAsync())
            .ReturnsAsync(new CleanupResult
            {
                Success = true,
                AnalysisRunsDeleted = 5,
                AnalysisRunsKept = 3,
                Message = "Cleanup successful",
                DeletedRunDetails = new List<string> { "Run #1", "Run #2" }
            });

        var controller = new AnalysisController(
            NullLogger<AnalysisController>.Instance,
            new Mock<IAnalysisOrchestrationService>(MockBehavior.Strict).Object,
            new Mock<IAnalysisJobService>(MockBehavior.Strict).Object,
            cleanup.Object,
            new ConfigurationBuilder().Build());

        var result = await controller.CleanupOldAnalysisRuns();

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(ok.Value);
        cleanup.Verify(c => c.CleanupOldAnalysisRunsAsync(), Times.Once);
    }

    [Fact]
    public async Task CleanupOldAnalysisRuns_ServiceFails_Returns500()
    {
        var cleanup = new Mock<IDataCleanupService>();
        cleanup
            .Setup(c => c.CleanupOldAnalysisRunsAsync())
            .ReturnsAsync(new CleanupResult
            {
                Success = false,
                Message = "Database error",
                ErrorDetails = "Connection failed"
            });

        var controller = new AnalysisController(
            NullLogger<AnalysisController>.Instance,
            new Mock<IAnalysisOrchestrationService>(MockBehavior.Strict).Object,
            new Mock<IAnalysisJobService>(MockBehavior.Strict).Object,
            cleanup.Object,
            new ConfigurationBuilder().Build());

        var result = await controller.CleanupOldAnalysisRuns();

        var status = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, status.StatusCode);
    }

    [Fact]
    public async Task CleanupOldAnalysisRuns_ServiceThrows_Returns500()
    {
        var cleanup = new Mock<IDataCleanupService>();
        cleanup
            .Setup(c => c.CleanupOldAnalysisRunsAsync())
            .ThrowsAsync(new Exception("Unexpected error"));

        var controller = new AnalysisController(
            NullLogger<AnalysisController>.Instance,
            new Mock<IAnalysisOrchestrationService>(MockBehavior.Strict).Object,
            new Mock<IAnalysisJobService>(MockBehavior.Strict).Object,
            cleanup.Object,
            new ConfigurationBuilder().Build());

        var result = await controller.CleanupOldAnalysisRuns();

        var status = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, status.StatusCode);
    }

    private sealed class StubAnalysisJobService : IAnalysisJobService
    {
        public Dictionary<Guid, AnalysisJobStatusDto> StatusByJobId { get; } = new();

        public Guid StartAnalysis(string subscriptionId) => Guid.Empty;

        public bool TryGetStatus(Guid jobId, out AnalysisJobStatusDto? status)
        {
            if (StatusByJobId.TryGetValue(jobId, out var dto))
            {
                status = dto;
                return true;
            }

            status = null;
            return false;
        }
    }
}
