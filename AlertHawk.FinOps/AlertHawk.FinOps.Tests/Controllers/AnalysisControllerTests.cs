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
        var config = new ConfigurationBuilder().AddInMemoryCollection().Build();
        var controller = new AnalysisController(
            NullLogger<AnalysisController>.Instance,
            orchestration.Object,
            config);

        var result = await controller.StartAnalysis(subscriptionId!);

        Assert.IsType<BadRequestObjectResult>(result);
        orchestration.Verify(
            o => o.RunAnalysisForSingleSubscriptionAsync(It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public async Task StartAnalysis_ValidId_ReturnsOkWithPayload()
    {
        var orchestration = new Mock<IAnalysisOrchestrationService>();
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
            config);

        var result = await controller.StartAnalysis("sub-a");

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(ok.Value);
    }

    [Fact]
    public async Task StartAnalysis_WhenOrchestrationThrows_Returns500()
    {
        var orchestration = new Mock<IAnalysisOrchestrationService>();
        orchestration
            .Setup(o => o.RunAnalysisForSingleSubscriptionAsync("sub-x"))
            .ThrowsAsync(new InvalidOperationException("boom"));

        var config = new ConfigurationBuilder().AddInMemoryCollection().Build();
        var controller = new AnalysisController(
            NullLogger<AnalysisController>.Instance,
            orchestration.Object,
            config);

        var result = await controller.StartAnalysis("sub-x");

        var status = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, status.StatusCode);
    }
}
