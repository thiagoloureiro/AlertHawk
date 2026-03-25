using System.Text.Json;
using FinOpsToolSample.Services;

namespace AlertHawk.FinOps.Tests.Services;

public class HistoricalCostServiceTests
{
    private static List<HistoricalCostData> ParseRowsJson(string rowsJsonArray, string subscriptionId = "sub-1")
    {
        using var doc = JsonDocument.Parse(rowsJsonArray);
        return HistoricalCostQueryResponseParser.ParseRows(doc.RootElement, subscriptionId);
    }

    public class ParseRows
    {
        [Fact]
        public void EmptyArray_ReturnsEmptyList()
        {
            var list = ParseRowsJson("[]");
            Assert.Empty(list);
        }

        [Fact]
        public void FullRow_WithNumericYyyyMmDd_ParsesAllFields()
        {
            var list = ParseRowsJson(
                """[[12.5,20250315,"rg-prod","Microsoft.Storage"]]""",
                "my-sub");

            Assert.Single(list);
            var r = list[0];
            Assert.Equal("my-sub", r.SubscriptionId);
            Assert.Equal(12.5m, r.Cost);
            Assert.Equal(new DateTime(2025, 3, 15), r.Date.Date);
            Assert.Equal("rg-prod", r.ResourceGroup);
            Assert.Equal("Microsoft.Storage", r.ServiceName);
        }

        [Fact]
        public void DateAsString_UsesTryParse()
        {
            var list = ParseRowsJson("""[[1,"2030-06-15","a","b"]]""");
            Assert.Equal(2030, list[0].Date.Year);
            Assert.Equal(6, list[0].Date.Month);
            Assert.Equal(15, list[0].Date.Day);
        }

        [Fact]
        public void CostOnly_LeavesDateDefaultAndUnknownDimensions()
        {
            var list = ParseRowsJson("[[99]]");
            Assert.Single(list);
            Assert.Equal(99m, list[0].Cost);
            Assert.Equal(default, list[0].Date);
            Assert.Equal(string.Empty, list[0].ResourceGroup);
            Assert.Equal(string.Empty, list[0].ServiceName);
        }
    }

    public class NextLink
    {
        [Fact]
        public void TryExtractSkipToken_ReturnsSubstringAfterMarker()
        {
            var token = HistoricalCostQueryResponseParser.TryExtractSkipTokenFromNextLink(
                "https://management.azure.com/foo?api-version=1&$skiptoken=abc%3D%3D");

            Assert.Equal("abc%3D%3D", token);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("https://no-token-here")]
        public void TryExtractSkipToken_ReturnsNullWhenMissing(string? link)
        {
            Assert.Null(HistoricalCostQueryResponseParser.TryExtractSkipTokenFromNextLink(link));
        }

        [Fact]
        public void TryGetNextSkipToken_ReadsFromPropertiesObject()
        {
            using var doc = JsonDocument.Parse(
                """{"nextLink":"https://x/query?$skiptoken=page2"}""");

            var token = HistoricalCostQueryResponseParser.TryGetNextSkipToken(doc.RootElement);

            Assert.Equal("page2", token);
        }

        [Fact]
        public void TryGetNextSkipToken_NoNextLink_ReturnsNull()
        {
            using var doc = JsonDocument.Parse("""{"rows":[]}""");
            Assert.Null(HistoricalCostQueryResponseParser.TryGetNextSkipToken(doc.RootElement));
        }
    }
}
