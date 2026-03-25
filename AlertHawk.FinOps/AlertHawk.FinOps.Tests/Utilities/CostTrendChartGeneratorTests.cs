using System.Globalization;
using System.IO;
using FinOpsToolSample.Utilities;

namespace AlertHawk.FinOps.Tests.Utilities;

public class CostTrendChartGeneratorTests
{
    public class ComputeNormalizedRows
    {
        [Fact]
        public void EmptyCosts_ReturnsEmpty()
        {
            var rows = CostTrendAsciiChart.ComputeNormalizedRows(Array.Empty<decimal>(), chartHeight: 10);
            Assert.Empty(rows);
        }

        [Fact]
        public void IdenticalCosts_MapToZeroRow()
        {
            var rows = CostTrendAsciiChart.ComputeNormalizedRows([50m, 50m, 50m], chartHeight: 10);
            Assert.Equal(3, rows.Count);
            Assert.All(rows, r => Assert.Equal(0, r));
        }

        [Fact]
        public void MinAndMaxCosts_MapToEndsOfScale()
        {
            var rows = CostTrendAsciiChart.ComputeNormalizedRows([0m, 100m], chartHeight: 10);
            Assert.Equal(2, rows.Count);
            Assert.Equal(0, rows[0]);
            Assert.Equal(9, rows[1]);
        }

        [Fact]
        public void ThreePoints_SpreadsAcrossRows()
        {
            var rows = CostTrendAsciiChart.ComputeNormalizedRows([0m, 50m, 100m], chartHeight: 5);
            Assert.Equal(new[] { 0, 2, 4 }, rows);
        }
    }

    public class WriteChart
    {
        [Fact]
        public void WritesAxisLabelsAndDateRange()
        {
            using var writer = new StringWriter(CultureInfo.InvariantCulture);
            var d0 = new DateTime(2025, 3, 1, 0, 0, 0, DateTimeKind.Utc);
            var d1 = new DateTime(2025, 3, 5, 0, 0, 0, DateTimeKind.Utc);
            var data = new List<(DateTime Date, decimal Cost)>
            {
                (d0, 10m),
                (d0.AddDays(1), 20m),
                (d1, 15m)
            };

            CostTrendAsciiChart.WriteChart(writer, data, chartHeight: 4, chartWidth: 10);

            var text = writer.ToString();
            Assert.Contains('●', text);
            Assert.Contains('│', text);
            Assert.Contains("03/01", text);
            Assert.Contains("03/05", text);
            Assert.Contains('└', text);
        }

        [Fact]
        public void EmptyData_WritesNothing()
        {
            using var writer = new StringWriter(CultureInfo.InvariantCulture);
            CostTrendAsciiChart.WriteChart(writer, Array.Empty<(DateTime, decimal)>());
            Assert.Equal(string.Empty, writer.ToString());
        }
    }
}
