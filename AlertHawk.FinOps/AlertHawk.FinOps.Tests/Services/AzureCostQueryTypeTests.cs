using FinOpsToolSample.Configuration;
using FinOpsToolSample.Services;
using System.Text.Json;

namespace AlertHawk.FinOps.Tests.Services;

public class AzureCostQueryTypeTests
{
    [Theory]
    [InlineData(null, AzureCostQueryType.ActualCost)]
    [InlineData("", AzureCostQueryType.ActualCost)]
    [InlineData("ActualCost", AzureCostQueryType.ActualCost)]
    [InlineData("actualcost", AzureCostQueryType.ActualCost)]
    [InlineData("AmortizedCost", AzureCostQueryType.AmortizedCost)]
    [InlineData("AMORTIZEDCOST", AzureCostQueryType.AmortizedCost)]
    [InlineData("Usage", AzureCostQueryType.ActualCost)]
    public void Normalize_MapsToSupportedTypes(string? input, string expected)
    {
        Assert.Equal(expected, AzureCostQueryType.Normalize(input));
    }

    [Fact]
    public void DisplayLabel_Amortized_ReturnsAmortized()
    {
        Assert.Equal("Amortized", AzureCostQueryType.DisplayLabel(AzureCostQueryType.AmortizedCost));
        Assert.Equal("Actual", AzureCostQueryType.DisplayLabel(AzureCostQueryType.ActualCost));
    }
}

public class CostManagementQueryBuilderTests
{
    [Fact]
    public void BuildMonthToDateQuery_UsesAmortizedCostWhenConfigured()
    {
        var payload = CostManagementQueryBuilder.BuildMonthToDateQuery(AzureCostQueryType.AmortizedCost);
        using var doc = JsonDocument.Parse(JsonSerializer.Serialize(payload));

        Assert.Equal(
            AzureCostQueryType.AmortizedCost,
            doc.RootElement.GetProperty("type").GetString());
        Assert.Equal("MonthToDate", doc.RootElement.GetProperty("timeframe").GetString());
        Assert.Equal(
            "PreTaxCost",
            doc.RootElement.GetProperty("dataset").GetProperty("aggregation").GetProperty("totalCost").GetProperty("name").GetString());
    }

    [Fact]
    public void BuildCustomDailyQuery_DefaultsInvalidTypeToActualCost()
    {
        var start = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var end = new DateTime(2025, 6, 30, 0, 0, 0, DateTimeKind.Utc);
        var payload = CostManagementQueryBuilder.BuildCustomDailyQuery("invalid", start, end);
        using var doc = JsonDocument.Parse(JsonSerializer.Serialize(payload));

        Assert.Equal(AzureCostQueryType.ActualCost, doc.RootElement.GetProperty("type").GetString());
        Assert.Equal("Custom", doc.RootElement.GetProperty("timeframe").GetString());
        Assert.Equal("2025-01-01T00:00:00Z", doc.RootElement.GetProperty("timePeriod").GetProperty("from").GetString());
    }
}
