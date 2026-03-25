using AlertHawk.FinOps.Tests.Infrastructure;
using FinOpsToolSample.Configuration;
using FinOpsToolSample.Services;
using Microsoft.Extensions.Options;

namespace AlertHawk.FinOps.Tests.Services;

public class AnalysisOrchestrationServiceTests
{
    [Fact]
    public async Task RunAnalysisAsync_NoSubscriptionIds_ReturnsFailureWithMessage()
    {
        await using var db = FinOpsDbContextFactory.Create();
        var databaseService = new DatabaseService(db);
        var azure = Options.Create(new AzureConfiguration
        {
            TenantId = "11111111-1111-1111-1111-111111111111",
            ClientId = "22222222-2222-2222-2222-222222222222",
            ClientSecret = "not-used-when-no-subscriptions",
            SubscriptionIds = ""
        });
        var ai = Options.Create(new AIConfiguration());
        var svc = new AnalysisOrchestrationService(databaseService, azure, ai);

        var result = await svc.RunAnalysisAsync();

        Assert.False(result.Success);
        Assert.Contains("No subscription IDs configured", result.Message);
    }
}
