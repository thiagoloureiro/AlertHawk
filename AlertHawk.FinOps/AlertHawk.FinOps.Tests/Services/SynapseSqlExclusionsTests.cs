using FinOpsToolSample.Services;

namespace AlertHawk.FinOps.Tests.Services;

public class SynapseSqlExclusionsTests
{
    [Theory]
    [InlineData(
        "/subscriptions/s/resourceGroups/rg/providers/Microsoft.Sql/servers/ws1/databases/db",
        "/subscriptions/s/resourceGroups/rg/providers/Microsoft.Sql/servers/ws1",
        "ws1")]
    [InlineData(
        "/subscriptions/s/resourceGroups/rg/providers/microsoft.sql/servers/MyServer/databases/master",
        "/subscriptions/s/resourceGroups/rg/providers/microsoft.sql/servers/MyServer",
        "MyServer")]
    public void TryGetSqlServerResourceIdAndName_ValidDatabaseId_ParsesServer(
        string databaseId,
        string expectedServerId,
        string expectedServerName)
    {
        var ok = SynapseSqlExclusions.TryGetSqlServerResourceIdAndName(
            databaseId,
            out var serverId,
            out var serverName);

        Assert.True(ok);
        Assert.Equal(expectedServerId, serverId);
        Assert.Equal(expectedServerName, serverName);
    }

    [Theory]
    [InlineData("")]
    [InlineData("/subscriptions/s/resourceGroups/rg/providers/Microsoft.Storage/storageAccounts/sa")]
    [InlineData("/subscriptions/s/providers/Microsoft.Sql/servers/only-server")]
    [InlineData("/subscriptions/s/resourceGroups/rg/providers/Microsoft.Sql/servers/")]
    public void TryGetSqlServerResourceIdAndName_InvalidId_ReturnsFalse(string databaseId)
    {
        var ok = SynapseSqlExclusions.TryGetSqlServerResourceIdAndName(
            databaseId,
            out var serverId,
            out var serverName);

        Assert.False(ok);
        Assert.Equal(string.Empty, serverId);
        Assert.Equal(string.Empty, serverName);
    }

    [Fact]
    public void CreateForTests_StoresServerIdsAndWorkspaceNames()
    {
        var exclusions = SynapseSqlExclusions.CreateForTests(
            analyticsLinkedSqlServerResourceIds: ["/subscriptions/s/providers/Microsoft.Sql/servers/a"],
            synapseWorkspaceNames: ["workspace-1"]);

        Assert.Contains(
            "/subscriptions/s/providers/Microsoft.Sql/servers/a",
            exclusions.AnalyticsLinkedSqlServerResourceIds);
        Assert.Contains("workspace-1", exclusions.SynapseWorkspaceNames);
    }
}
