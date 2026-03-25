using System.Collections.Generic;
using System.Linq;

namespace FinOpsToolSample.Services;

/// <summary>
/// Pure metric aggregation/formatting used by <see cref="AppServiceAnalysisService"/> (unit-tested).
/// </summary>
internal static class AppServiceAnalysisMetrics
{
    internal static (double AvgValue, double TotalValue) SummarizeTimeSeriesPoints(
        IEnumerable<(double? Average, double? Total)> points)
    {
        var list = points as IReadOnlyList<(double? Average, double? Total)> ?? points.ToList();
        var avgValue = list
            .Where(v => v.Average.HasValue)
            .Select(v => v.Average!.Value)
            .DefaultIfEmpty(0)
            .Average();
        var totalValue = list
            .Where(v => v.Total.HasValue)
            .Select(v => v.Total!.Value)
            .DefaultIfEmpty(0)
            .Sum();
        return (avgValue, totalValue);
    }

    internal static string? FormatMetricLine(string metricName, double avgValue, double totalValue)
    {
        return metricName switch
        {
            "Requests" => $"    - Requests: Total = {totalValue:F0}",
            "MemoryWorkingSet" => $"    - Memory: Avg = {avgValue / 1024 / 1024:F2} MB",
            "Http5xx" => $"    - HTTP 5xx Errors: Total = {totalValue:F0}",
            "AverageResponseTime" => $"    - Response Time: Avg = {avgValue:F2} seconds",
            _ => null
        };
    }
}
