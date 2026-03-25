using System.Text.Json;
using FinOpsToolSample.Models;
using FinOpsToolSample.Services;

namespace AlertHawk.FinOps.Tests.Services;

public class CostManagementServiceTests
{
    private static (decimal Total, Dictionary<string, decimal> ByRg, List<ServiceCostDetail> BySvc) ParseRowsJson(
        string rowsJsonArray)
    {
        using var doc = JsonDocument.Parse(rowsJsonArray);
        return CostManagementQueryResultParser.ParseCostRows(doc.RootElement);
    }

    [Fact]
    public void ParseCostRows_EmptyArray_ReturnsZeroTotalsAndEmptyCollections()
    {
        var (total, byRg, bySvc) = ParseRowsJson("[]");

        Assert.Equal(0, total);
        Assert.Empty(byRg);
        Assert.Empty(bySvc);
    }

    [Fact]
    public void ParseCostRows_SingleValueOnly_UsesUnknownDimensions()
    {
        var (total, byRg, bySvc) = ParseRowsJson("[[42.5]]");

        Assert.Equal(42.5m, total);
        Assert.Single(byRg);
        Assert.Equal(42.5m, byRg["Unknown"]);
        Assert.Single(bySvc);
        Assert.Equal("Unknown", bySvc[0].ServiceName);
        Assert.Equal("Unknown", bySvc[0].ResourceGroup);
        Assert.Equal(42.5m, bySvc[0].Cost);
    }

    [Fact]
    public void ParseCostRows_FullRow_PopulatesResourceGroupAndService()
    {
        var (total, byRg, bySvc) = ParseRowsJson(
            """[[100,"20250101","rg-prod","Microsoft.Storage"]]""");

        Assert.Equal(100m, total);
        Assert.Equal(100m, byRg["rg-prod"]);
        Assert.Single(bySvc);
        Assert.Equal("Microsoft.Storage", bySvc[0].ServiceName);
        Assert.Equal("rg-prod", bySvc[0].ResourceGroup);
    }

    [Fact]
    public void ParseCostRows_MultipleRowsSameResourceGroup_AggregatesTotal()
    {
        var (total, byRg, bySvc) = ParseRowsJson(
            """
            [
              [10,"d","shared-rg","A"],
              [30,"d","shared-rg","B"],
              [5,"d","other-rg","C"]
            ]
            """);

        Assert.Equal(45m, total);
        Assert.Equal(40m, byRg["shared-rg"]);
        Assert.Equal(5m, byRg["other-rg"]);
        Assert.Equal(3, bySvc.Count);
    }

    [Fact]
    public void ParseCostRows_NullStringDimensions_UseUnknown()
    {
        var (total, _, bySvc) = ParseRowsJson(
            """[[7.5,"x",null,null]]""");

        Assert.Equal(7.5m, total);
        Assert.Single(bySvc);
        Assert.Equal("Unknown", bySvc[0].ResourceGroup);
        Assert.Equal("Unknown", bySvc[0].ServiceName);
    }
}
