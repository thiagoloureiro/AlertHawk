using FinOpsToolSample.Services;

namespace AlertHawk.FinOps.Tests.Services;

public class AzureManagementHttpTests
{
    [Fact]
    public void Shared_IsConfiguredWithJsonAcceptAndLongTimeout()
    {
        var client = AzureManagementHttp.Shared;

        Assert.NotNull(client);
        Assert.True(client.Timeout >= TimeSpan.FromMinutes(10));
        Assert.Contains(
            client.DefaultRequestHeaders.Accept,
            h => h.MediaType == "application/json");
    }
}
