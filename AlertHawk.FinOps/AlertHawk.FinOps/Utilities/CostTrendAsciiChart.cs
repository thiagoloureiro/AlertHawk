using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FinOpsToolSample.Utilities;

/// <summary>
/// ASCII cost trend chart used by <see cref="CostTrendChartGenerator"/> (unit-tested).
/// </summary>
internal static class CostTrendAsciiChart
{
    internal static IReadOnlyList<int> ComputeNormalizedRows(IReadOnlyList<decimal> costs, int chartHeight)
    {
        if (costs.Count == 0)
        {
            return Array.Empty<int>();
        }

        var maxCost = costs.Max();
        var minCost = costs.Min();
        var range = maxCost - minCost;
        if (range == 0)
        {
            range = 1;
        }

        return costs
            .Select(c => (int)(((c - minCost) / range) * (chartHeight - 1)))
            .ToList();
    }

    internal static void WriteChart(
        TextWriter writer,
        IReadOnlyList<(DateTime Date, decimal Cost)> data,
        int chartHeight = 10,
        int chartWidth = 50)
    {
        if (data.Count == 0)
        {
            return;
        }

        var costs = data.Select(d => d.Cost).ToList();
        var maxCost = costs.Max();
        var minCost = costs.Min();
        var range = maxCost - minCost;
        if (range == 0)
        {
            range = 1;
        }

        var points = ComputeNormalizedRows(costs, chartHeight);

        for (var row = chartHeight - 1; row >= 0; row--)
        {
            var yValue = minCost + range * row / (chartHeight - 1);
            writer.Write($"${yValue,8:F0} |");

            for (var col = 0; col < Math.Min(points.Count, chartWidth); col++)
            {
                if (points[col] == row)
                {
                    writer.Write("●");
                }
                else if (points[col] > row)
                {
                    writer.Write("│");
                }
                else
                {
                    writer.Write(" ");
                }
            }

            writer.WriteLine();
        }

        writer.Write("         └");
        writer.WriteLine(new string('─', Math.Min(data.Count, chartWidth)));
        writer.WriteLine($"         {data[0].Date:MM/dd} → {data[^1].Date:MM/dd}");
        writer.WriteLine();
    }
}
