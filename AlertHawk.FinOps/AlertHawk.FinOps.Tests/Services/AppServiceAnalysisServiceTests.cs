using FinOpsToolSample.Services;

namespace AlertHawk.FinOps.Tests.Services;

public class AppServiceAnalysisServiceTests
{
    public class SummarizeTimeSeriesPoints
    {
        [Fact]
        public void EmptySequence_ReturnsZeroAverageAndZeroTotal()
        {
            var (avg, total) = AppServiceAnalysisMetrics.SummarizeTimeSeriesPoints([]);

            Assert.Equal(0, avg);
            Assert.Equal(0, total);
        }

        [Fact]
        public void OnlyAverages_ComputesAverageIgnoresMissingTotals()
        {
            var (avg, total) = AppServiceAnalysisMetrics.SummarizeTimeSeriesPoints(
            [
                (10.0, (double?)null),
                (20.0, (double?)null)
            ]);

            Assert.Equal(15, avg);
            Assert.Equal(0, total);
        }

        [Fact]
        public void OnlyTotals_SumsTotalsAverageOfPresentAveragesUsesZeroDefault()
        {
            var (avg, total) = AppServiceAnalysisMetrics.SummarizeTimeSeriesPoints(
            [
                ((double?)null, 3.0),
                ((double?)null, 7.0)
            ]);

            Assert.Equal(0, avg);
            Assert.Equal(10, total);
        }

        [Fact]
        public void MixedPoints_AggregatesBoth()
        {
            var (avg, total) = AppServiceAnalysisMetrics.SummarizeTimeSeriesPoints(
            [
                (4.0, 100.0),
                (8.0, 50.0)
            ]);

            Assert.Equal(6, avg);
            Assert.Equal(150, total);
        }
    }

    public class FormatMetricLine
    {
        [Theory]
        [InlineData("Requests", 0, 42, "    - Requests: Total = 42")]
        [InlineData("Http5xx", 0, 3, "    - HTTP 5xx Errors: Total = 3")]
        public void KnownTotalMetrics_UseTotalValue(string name, double avg, double total, string expected)
        {
            var line = AppServiceAnalysisMetrics.FormatMetricLine(name, avg, total);
            Assert.Equal(expected, line);
        }

        [Fact]
        public void MemoryWorkingSet_ConvertsBytesToMegabytes()
        {
            var bytes = 2 * 1024 * 1024;
            var line = AppServiceAnalysisMetrics.FormatMetricLine("MemoryWorkingSet", bytes, 0);
            Assert.Equal("    - Memory: Avg = 2.00 MB", line);
        }

        [Fact]
        public void AverageResponseTime_UsesAverageInSeconds()
        {
            var line = AppServiceAnalysisMetrics.FormatMetricLine("AverageResponseTime", 1.25, 0);
            Assert.Equal("    - Response Time: Avg = 1.25 seconds", line);
        }

        [Fact]
        public void UnknownMetric_ReturnsNull()
        {
            Assert.Null(AppServiceAnalysisMetrics.FormatMetricLine("CpuPercentage", 1, 2));
        }
    }
}
