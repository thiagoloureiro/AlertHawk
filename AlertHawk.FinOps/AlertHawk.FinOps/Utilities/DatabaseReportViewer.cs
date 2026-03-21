using FinOpsToolSample.Data;
using FinOpsToolSample.Services;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace FinOpsToolSample.Utilities
{
    public class DatabaseReportViewer
    {
        private readonly DatabaseService _dbService;

        public DatabaseReportViewer(DatabaseService dbService)
        {
            _dbService = dbService;
        }

        public async Task ShowLatestAnalysisAsync(string subscriptionId)
        {
            Console.WriteLine("\n╔═══════════════════════════════════════════════════════╗");
            Console.WriteLine("║         Latest Analysis from Database                 ║");
            Console.WriteLine("╚═══════════════════════════════════════════════════════╝\n");

            var analysis = await _dbService.GetLatestAnalysisAsync(subscriptionId);
            
            if (analysis == null)
            {
                Console.WriteLine("No analysis found for this subscription.");
                return;
            }

            Console.WriteLine($"📊 Analysis Run ID: {analysis.Id}");
            Console.WriteLine($"📅 Date: {analysis.RunDate:yyyy-MM-dd HH:mm:ss}");
            Console.WriteLine($"💰 Total Cost: ${analysis.TotalMonthlyCost:F2}");
            Console.WriteLine($"📦 Resources Analyzed: {analysis.TotalResourcesAnalyzed}");
            Console.WriteLine($"🤖 AI Model: {analysis.AiModel}");
            Console.WriteLine();

            // Show top costs
            Console.WriteLine("💰 Top Costs:");
            var topCosts = analysis.CostDetails
                .OrderByDescending(c => c.Cost)
                .Take(5);

            foreach (var cost in topCosts)
            {
                Console.WriteLine($"  {cost.CostType}: {cost.Name} - ${cost.Cost:F2}");
            }
            Console.WriteLine();

            // Show flagged resources
            var flaggedResources = analysis.Resources
                .Where(r => !string.IsNullOrEmpty(r.Flags))
                .ToList();

            if (flaggedResources.Any())
            {
                Console.WriteLine($"⚠️  Flagged Resources ({flaggedResources.Count}):");
                foreach (var resource in flaggedResources.Take(5))
                {
                    Console.WriteLine($"  {resource.ResourceName} ({resource.ResourceType})");
                    Console.WriteLine($"    {resource.Flags}");
                }
                Console.WriteLine();
            }

            // Show AI recommendations
            var aiRec = analysis.AiRecommendations.FirstOrDefault();
            if (aiRec != null)
            {
                Console.WriteLine("🤖 AI Recommendations:");

                // Show summary
                if (!string.IsNullOrEmpty(aiRec.Summary))
                {
                    Console.WriteLine($"\nSummary: {aiRec.Summary}");
                }

                Console.WriteLine("\nFull recommendations available in database.");
                Console.WriteLine($"To view full text, use: GetFullRecommendationsAsync({analysis.Id})");
                Console.WriteLine();
            }
        }

        public async Task ShowFullRecommendationsAsync(int analysisRunId)
        {
            Console.WriteLine("\n╔═══════════════════════════════════════════════════════╗");
            Console.WriteLine("║         Full AI Recommendations                       ║");
            Console.WriteLine("╚═══════════════════════════════════════════════════════╝\n");

            var analysis = await _dbService.GetLatestAnalysisAsync("");
            if (analysis?.AiRecommendations == null || !analysis.AiRecommendations.Any())
            {
                Console.WriteLine("No recommendations found.");
                return;
            }

            var recommendation = analysis.AiRecommendations.First();
            var formattedText = MarkdownFormatter.FormatForConsole(recommendation.RecommendationText);

            Console.WriteLine(formattedText);
            Console.WriteLine();
        }

        public async Task ShowCostTrendAsync(string subscriptionId, int days = 30)
        {
            Console.WriteLine($"\n💹 Cost Trend (Last {days} days):\n");

            var history = await _dbService.GetAnalysisHistoryAsync(subscriptionId, 10);
            
            if (!history.Any())
            {
                Console.WriteLine("No historical data available.");
                return;
            }

            Console.WriteLine($"{"Date",-20} {"Total Cost",-15} {"Resources",-15}");
            Console.WriteLine(new string('-', 50));

            foreach (var run in history)
            {
                Console.WriteLine($"{run.RunDate:yyyy-MM-dd HH:mm}    ${run.TotalMonthlyCost,-12:F2}  {run.TotalResourcesAnalyzed,-15}");
            }
            Console.WriteLine();
        }

        public async Task ShowResourceSummaryAsync(string subscriptionId)
        {
            Console.WriteLine("\n📊 Resource Summary:\n");

            var summary = await _dbService.GetResourceCountByTypeAsync(subscriptionId);
            
            if (!summary.Any())
            {
                Console.WriteLine("No resource data available.");
                return;
            }

            Console.WriteLine($"{"Resource Type",-30} {"Count",-10}");
            Console.WriteLine(new string('-', 40));

            foreach (var kvp in summary.OrderByDescending(x => x.Value))
            {
                Console.WriteLine($"{kvp.Key,-30} {kvp.Value,-10}");
            }
            Console.WriteLine();
        }
    }
}
