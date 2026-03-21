using FinOpsToolSample.Data.Entities;
using FinOpsToolSample.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FinOpsToolSample.Utilities
{
    public class CostTrendChartGenerator
    {
        private readonly DatabaseService _dbService;

        public CostTrendChartGenerator(DatabaseService dbService)
        {
            _dbService = dbService;
        }

        public async Task ShowDailyCostTrendAsync(string subscriptionId, int days = 30)
        {
            Console.WriteLine($"\n📈 Daily Cost Trend (Last {days} days)\n");

            var data = await _dbService.GetDailyCostTrendAsync(subscriptionId, days);

            if (!data.Any())
            {
                Console.WriteLine("No historical data available.");
                return;
            }

            // Calculate statistics
            var avgCost = data.Average(d => d.Cost);
            var maxCost = data.Max(d => d.Cost);
            var minCost = data.Min(d => d.Cost);
            var totalCost = data.Sum(d => d.Cost);

            Console.WriteLine($"Statistics:");
            Console.WriteLine($"  Total: ${totalCost:F2}");
            Console.WriteLine($"  Average: ${avgCost:F2}");
            Console.WriteLine($"  Max: ${maxCost:F2}");
            Console.WriteLine($"  Min: ${minCost:F2}");
            Console.WriteLine();

            // ASCII chart
            Console.WriteLine("Daily Trend:");
            DrawAsciiChart(data.Select(d => (d.CostDate, d.Cost)).ToList());
        }

        public async Task ShowMonthlyCostTrendAsync(string subscriptionId, int months = 6)
        {
            Console.WriteLine($"\n📊 Monthly Cost Trend (Last {months} months)\n");

            var monthlyData = await _dbService.GetMonthlySpendingAsync(subscriptionId, months);

            if (!monthlyData.Any())
            {
                Console.WriteLine("No historical data available.");
                return;
            }

            var totalCost = monthlyData.Values.Sum();
            var avgCost = monthlyData.Values.Average();

            Console.WriteLine($"Total Spending: ${totalCost:F2}");
            Console.WriteLine($"Average Monthly: ${avgCost:F2}");
            Console.WriteLine();

            Console.WriteLine($"{"Month",-15} {"Cost",-15} {"Bar"}");
            Console.WriteLine(new string('-', 60));

            var maxCost = monthlyData.Values.Max();
            foreach (var kvp in monthlyData.OrderBy(x => x.Key))
            {
                var barLength = maxCost > 0 ? (int)((kvp.Value / maxCost) * 30) : 0;
                var bar = new string('█', barLength);
                Console.WriteLine($"{kvp.Key,-15} ${kvp.Value,-13:F2} {bar}");
            }
            Console.WriteLine();
        }

        public async Task ShowTopCostResourceGroupsAsync(string subscriptionId, int days = 30)
        {
            Console.WriteLine($"\n🏢 Top Cost Resource Groups (Last {days} days)\n");

            var data = await _dbService.GetCostTrendAsync(subscriptionId, "ResourceGroup", days);

            if (!data.Any())
            {
                Console.WriteLine("No data available.");
                return;
            }

            var grouped = data
                .GroupBy(c => c.Name)
                .Select(g => new
                {
                    Name = g.Key,
                    TotalCost = g.Sum(x => x.Cost)
                })
                .OrderByDescending(x => x.TotalCost)
                .Take(10)
                .ToList();

            Console.WriteLine($"{"Resource Group",-40} {"Total Cost",-15}");
            Console.WriteLine(new string('-', 55));

            foreach (var item in grouped)
            {
                Console.WriteLine($"{item.Name,-40} ${item.TotalCost,-13:F2}");
            }
            Console.WriteLine();
        }

        public async Task ShowCostComparisonAsync(string subscriptionId)
        {
            Console.WriteLine("\n📊 Cost Comparison (Month over Month)\n");

            var monthlyData = await _dbService.GetMonthlySpendingAsync(subscriptionId, 6);

            if (monthlyData.Count < 2)
            {
                Console.WriteLine("Need at least 2 months of data for comparison.");
                return;
            }

            var months = monthlyData.OrderBy(x => x.Key).ToList();

            Console.WriteLine($"{"Month",-15} {"Cost",-15} {"Change",-15} {"% Change"}");
            Console.WriteLine(new string('-', 60));

            for (int i = 0; i < months.Count; i++)
            {
                var current = months[i];
                var change = "";
                var percentChange = "";

                if (i > 0)
                {
                    var previous = months[i - 1];
                    var diff = current.Value - previous.Value;
                    var percent = previous.Value > 0 ? (diff / previous.Value) * 100 : 0;

                    change = $"{(diff >= 0 ? "+" : "")}{diff:F2}";
                    percentChange = $"{(percent >= 0 ? "+" : "")}{percent:F1}%";

                    var arrow = diff > 0 ? "↑" : diff < 0 ? "↓" : "→";
                    change += $" {arrow}";
                }

                Console.WriteLine($"{current.Key,-15} ${current.Value,-13:F2} {change,-15} {percentChange}");
            }
            Console.WriteLine();
        }

        public async Task ShowResourceGroupCostBreakdownAsync(string subscriptionId, string resourceGroup, int days = 30)
        {
            Console.WriteLine($"\n🏢 Cost Breakdown for Resource Group: {resourceGroup}\n");
            Console.WriteLine($"Period: Last {days} days\n");

            // Get total cost for the resource group
            var rgCosts = await _dbService.GetCostByResourceGroupTrendAsync(subscriptionId, resourceGroup, days);

            if (!rgCosts.Any())
            {
                Console.WriteLine("No data available for this resource group.");
                return;
            }

            var totalRgCost = rgCosts.Sum(c => c.Cost);
            var avgDailyCost = rgCosts.Average(c => c.Cost);

            Console.WriteLine($"Total Cost: ${totalRgCost:F2}");
            Console.WriteLine($"Average Daily: ${avgDailyCost:F2}");
            Console.WriteLine();

            // Get service breakdown
            var services = await _dbService.GetTopServicesInResourceGroupAsync(subscriptionId, resourceGroup, days);

            if (services.Any())
            {
                Console.WriteLine("Service Breakdown:");
                Console.WriteLine($"{"Service",-40} {"Cost",-15} {"% of Total"}");
                Console.WriteLine(new string('-', 60));

                foreach (var svc in services.OrderByDescending(x => x.Value).Take(10))
                {
                    var percentage = totalRgCost > 0 ? (svc.Value / totalRgCost) * 100 : 0;
                    Console.WriteLine($"{svc.Key,-40} ${svc.Value,-13:F2} {percentage,6:F1}%");
                }
                Console.WriteLine();
            }
        }

        public async Task ShowResourceGroupComparisonAsync(string subscriptionId, int days = 30)
        {
            Console.WriteLine($"\n🏢 Resource Group Cost Comparison (Last {days} days)\n");

            var data = await _dbService.GetCostTrendAsync(subscriptionId, "ResourceGroup", days);

            if (!data.Any())
            {
                Console.WriteLine("No data available.");
                return;
            }

            var grouped = data
                .GroupBy(c => c.ResourceGroup)
                .Select(g => new
                {
                    ResourceGroup = g.Key ?? "Unknown",
                    TotalCost = g.Sum(x => x.Cost),
                    AvgDailyCost = g.Average(x => x.Cost)
                })
                .OrderByDescending(x => x.TotalCost)
                .Take(10)
                .ToList();

            var grandTotal = grouped.Sum(x => x.TotalCost);

            Console.WriteLine($"{"Resource Group",-40} {"Total Cost",-15} {"Avg Daily",-15} {"% of Total"}");
            Console.WriteLine(new string('-', 75));

            foreach (var item in grouped)
            {
                var percentage = grandTotal > 0 ? (item.TotalCost / grandTotal) * 100 : 0;
                Console.WriteLine($"{item.ResourceGroup,-40} ${item.TotalCost,-13:F2} ${item.AvgDailyCost,-13:F2} {percentage,6:F1}%");
            }

            Console.WriteLine();
            Console.WriteLine($"Grand Total: ${grandTotal:F2}");
            Console.WriteLine();
        }

        private void DrawAsciiChart(List<(DateTime Date, decimal Cost)> data)
        {
            if (!data.Any()) return;

            const int chartHeight = 10;
            const int chartWidth = 50;

            var maxCost = data.Max(d => d.Cost);
            var minCost = data.Min(d => d.Cost);
            var range = maxCost - minCost;

            if (range == 0) range = 1;

            var points = new List<int>();
            foreach (var item in data)
            {
                var normalized = (int)(((item.Cost - minCost) / range) * (chartHeight - 1));
                points.Add(normalized);
            }

            // Draw chart
            for (int row = chartHeight - 1; row >= 0; row--)
            {
                var yValue = minCost + (range * row / (chartHeight - 1));
                Console.Write($"${yValue,8:F0} |");

                for (int col = 0; col < Math.Min(points.Count, chartWidth); col++)
                {
                    if (points[col] == row)
                        Console.Write("●");
                    else if (points[col] > row)
                        Console.Write("│");
                    else
                        Console.Write(" ");
                }
                Console.WriteLine();
            }

            Console.Write("         └");
            Console.WriteLine(new string('─', Math.Min(data.Count, chartWidth)));

            // Show date range
            if (data.Any())
            {
                Console.WriteLine($"         {data.First().Date:MM/dd} → {data.Last().Date:MM/dd}");
            }
            Console.WriteLine();
        }
    }
}
