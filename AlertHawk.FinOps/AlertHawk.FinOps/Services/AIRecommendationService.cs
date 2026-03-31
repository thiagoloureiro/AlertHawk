using FinOpsToolSample.Models;
using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace FinOpsToolSample.Services
{
    public class AIRecommendationService
    {
        private readonly string _apiKey;
        private readonly string _apiUrl;
        private readonly HttpClient _httpClient;

        public AIRecommendationService(
            string apiKey,
            string apiUrl,
            string apiKeyHeaderName,
            HttpClient? httpClient = null)
        {
            _apiKey = apiKey;
            _apiUrl = apiUrl;
            _httpClient = httpClient ?? new HttpClient();
            _httpClient.DefaultRequestHeaders.TryAddWithoutValidation(apiKeyHeaderName, _apiKey);
        }

        public async Task<(string recommendations, AIApiResponse? response)> GetRecommendationsAsync(AzureResourceData data)
        {
            Console.WriteLine("\n╔═══════════════════════════════════════════════════════╗");
            Console.WriteLine("║     Generating AI-Powered Recommendations             ║");
            Console.WriteLine("╚═══════════════════════════════════════════════════════╝");
            Console.WriteLine();

            try
            {
                var prompt = BuildComprehensivePrompt(data);

                Console.WriteLine("📤 Sending data to AI AI for analysis...");
                Console.WriteLine($"   Analyzing {data.Resources.Count} resources");
                Console.WriteLine($"   Total Monthly Cost: ${data.TotalMonthlyCost:F2}");
                Console.WriteLine();

                var request = new AIApiRequest
                {
                    input = prompt
                };

                Console.WriteLine("🔍 Prompt sent to AI:");
                Console.WriteLine(prompt);
                Console.WriteLine();

                var jsonRequest = JsonSerializer.Serialize(request);
                var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

                _httpClient.Timeout = TimeSpan.FromSeconds(240);
                var response = await _httpClient.PostAsync(_apiUrl, content);

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    throw new Exception($"AI API Error: {response.StatusCode} - {error}");
                }

                var responseJson = await response.Content.ReadAsStringAsync();
                var AIResponse = JsonSerializer.Deserialize<AIApiResponse>(responseJson);

                if (AIResponse?.output?.content != null)
                {
                    Console.WriteLine("✅ Recommendations received from AI AI");
                    Console.WriteLine();
                    Console.WriteLine("╔═══════════════════════════════════════════════════════╗");
                    Console.WriteLine("║            AI AI RECOMMENDATIONS                    ║");
                    Console.WriteLine("╚═══════════════════════════════════════════════════════╝");
                    Console.WriteLine();
                    Console.WriteLine(AIResponse.output.content);
                    Console.WriteLine();
                    Console.WriteLine($"📊 Conversation ID: {AIResponse.conversation_id}");
                    Console.WriteLine($"🤖 Model Used: {AIResponse.model}");

                    // Save to markdown file
                    await SaveRecommendationsToFile(data, AIResponse);

                    return (AIResponse.output.content, AIResponse);
                }

                return ("No recommendations received from AI AI.", null);
            }
            catch (Exception ex)
            {
                SentrySdk.CaptureException(ex);
                Console.WriteLine($"❌ Error getting recommendations from AI AI: {ex.Message}");
                return (string.Empty, null);
            }
        }

        private async Task<string> SaveRecommendationsToFile(AzureResourceData data, AIApiResponse response)
        {
            try
            {
                var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
                var filename = $"FinOps_Report_{timestamp}.md";

                var sb = new StringBuilder();

                // Header
                sb.AppendLine("# Azure FinOps Analysis Report");
                sb.AppendLine();
                sb.AppendLine($"**Generated:** {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                sb.AppendLine($"**Subscription:** {data.SubscriptionName}");
                sb.AppendLine($"**Subscription ID:** {data.SubscriptionId}");
                sb.AppendLine();
                sb.AppendLine("---");
                sb.AppendLine();

                // Executive Summary
                sb.AppendLine("## 📊 Executive Summary");
                sb.AppendLine();
                sb.AppendLine($"- **Total Monthly Cost (MTD):** ${data.TotalMonthlyCost:F2}");
                sb.AppendLine($"- **Resources Analyzed:** {data.Resources.Count}");
                sb.AppendLine($"- **AI Model:** {response.model}");
                sb.AppendLine($"- **Conversation ID:** {response.conversation_id}");
                sb.AppendLine();

                // Cost Breakdown
                sb.AppendLine("## 💰 Cost Breakdown");
                sb.AppendLine();
                sb.AppendLine("### Top Costs by Resource Group");
                sb.AppendLine();
                sb.AppendLine("| Resource Group | Cost |");
                sb.AppendLine("|----------------|------|");
                foreach (var rg in data.CostsByResourceGroup.OrderByDescending(x => x.Value).Take(10))
                {
                    sb.AppendLine($"| {rg.Key} | ${rg.Value:F2} |");
                }
                sb.AppendLine();

                sb.AppendLine("### Top Costs by Service");
                sb.AppendLine();
                sb.AppendLine("| Service | Cost |");
                sb.AppendLine("|---------|------|");
                var serviceAggregated = data.CostsByService
                    .GroupBy(s => s.ServiceName)
                    .Select(g => new { ServiceName = g.Key, Cost = g.Sum(x => x.Cost) })
                    .OrderByDescending(x => x.Cost)
                    .Take(10);
                foreach (var svc in serviceAggregated)
                {
                    sb.AppendLine($"| {svc.ServiceName} | ${svc.Cost:F2} |");
                }
                sb.AppendLine();

                // Resource Summary
                sb.AppendLine("## 🔍 Resource Summary");
                sb.AppendLine();
                var resourcesByType = data.Resources.GroupBy(r => r.Type);
                foreach (var group in resourcesByType)
                {
                    sb.AppendLine($"- **{group.Key}:** {group.Count()}");
                }
                sb.AppendLine();

                // Flagged Resources
                var flaggedResources = data.Resources.Where(r => r.Flags.Any()).ToList();
                if (flaggedResources.Any())
                {
                    sb.AppendLine("## ⚠️ Resources Requiring Attention");
                    sb.AppendLine();
                    foreach (var resource in flaggedResources)
                    {
                        sb.AppendLine($"### {resource.Name}");
                        sb.AppendLine($"- **Type:** {resource.Type}");
                        sb.AppendLine($"- **Resource Group:** {resource.ResourceGroup}");
                        sb.AppendLine($"- **Location:** {resource.Location}");
                        sb.AppendLine($"- **Issues:**");
                        foreach (var flag in resource.Flags)
                        {
                            sb.AppendLine($"  - {flag}");
                        }
                        sb.AppendLine();
                    }
                }

                // AI Recommendations
                sb.AppendLine("---");
                sb.AppendLine();
                sb.AppendLine("## 🤖 AI-Powered Recommendations");
                sb.AppendLine();
                sb.AppendLine(response.output.content);
                sb.AppendLine();

                // Footer
                sb.AppendLine("---");
                sb.AppendLine();
                sb.AppendLine("## 📝 Notes");
                sb.AppendLine();
                sb.AppendLine("- This report was generated automatically by the Azure FinOps Analysis Tool");
                sb.AppendLine("- Recommendations are based on 7-day performance metrics");
                sb.AppendLine("- Always validate recommendations before implementing changes");
                sb.AppendLine("- Cost estimates are approximate and may vary");
                sb.AppendLine();
                sb.AppendLine("---");
                sb.AppendLine($"*Report generated at {DateTime.Now:yyyy-MM-dd HH:mm:ss}*");

                await File.WriteAllTextAsync(filename, sb.ToString());

                Console.WriteLine();
                Console.WriteLine($"💾 Report saved to: {filename}");

                return filename;
            }
            catch (Exception ex)
            {
                SentrySdk.CaptureException(ex);
                Console.WriteLine($"⚠️  Warning: Could not save report to file: {ex.Message}");
                return string.Empty;
            }
        }

        private string BuildComprehensivePrompt(AzureResourceData data)
        {
            var sb = new StringBuilder();

            sb.AppendLine("I am a FinOps engineer analyzing Azure resources for cost optimization opportunities.");
            sb.AppendLine();
            sb.AppendLine("=== SUBSCRIPTION OVERVIEW ===");
            sb.AppendLine($"Subscription: {data.SubscriptionName}");
            sb.AppendLine($"Subscription ID: {data.SubscriptionId}");
            sb.AppendLine($"Total Monthly Cost (Month to Date): ${data.TotalMonthlyCost:F2}");
            sb.AppendLine();

            // Cost breakdown by resource group
            sb.AppendLine("=== TOP COSTS BY RESOURCE GROUP ===");
            var topRGs = data.CostsByResourceGroup
                .OrderByDescending(x => x.Value)
                .Take(10);
            foreach (var rg in topRGs)
            {
                sb.AppendLine($"- {rg.Key}: ${rg.Value:F2}");
            }
            sb.AppendLine();

            // Cost breakdown by service
            sb.AppendLine("=== TOP COSTS BY SERVICE ===");
            var topServices = data.CostsByService
                .GroupBy(s => s.ServiceName)
                .Select(g => new { ServiceName = g.Key, Cost = g.Sum(x => x.Cost) })
                .OrderByDescending(x => x.Cost)
                .Take(10);
            foreach (var svc in topServices)
            {
                sb.AppendLine($"- {svc.ServiceName}: ${svc.Cost:F2}");
            }
            sb.AppendLine();

            // Resource details by type
            var resourcesByType = data.Resources.GroupBy(r => r.Type);

            foreach (var group in resourcesByType)
            {
                sb.AppendLine($"=== {group.Key.ToUpper()} ({group.Count()}) ===");

                foreach (var resource in group)
                {
                    sb.AppendLine($"\n{resource.Name}:");
                    sb.AppendLine($"  Resource Group: {resource.ResourceGroup}");
                    sb.AppendLine($"  Location: {resource.Location}");

                    // Properties
                    if (resource.Properties.Any())
                    {
                        sb.AppendLine("  Properties:");
                        foreach (var prop in resource.Properties)
                        {
                            sb.AppendLine($"    - {prop.Key}: {prop.Value}");
                        }
                    }

                    // Metrics
                    if (resource.Metrics.Any())
                    {
                        sb.AppendLine("  Metrics (Last 7 Days):");
                        foreach (var metric in resource.Metrics)
                        {
                            sb.AppendLine($"    - {metric.Key}: {metric.Value:F2}");
                        }
                    }

                    // Flags (issues/notes)
                    if (resource.Flags.Any())
                    {
                        sb.AppendLine("  Flags:");
                        foreach (var flag in resource.Flags)
                        {
                            sb.AppendLine($"    - {flag}");
                        }
                    }
                }
                sb.AppendLine();
            }

            sb.AppendLine("=== REQUEST ===");
            sb.AppendLine("Based on the above Azure resource data, please provide:");
            sb.AppendLine("1. Cost optimization recommendations prioritized by potential savings");
            sb.AppendLine("2. Performance optimization suggestions");
            sb.AppendLine("3. Security and compliance recommendations");
            sb.AppendLine("4. Specific actions to take for each recommendation");
            sb.AppendLine("5. Estimated monthly cost savings for each recommendation");
            sb.AppendLine();
            sb.AppendLine("Please format the response in a clear, actionable manner with specific resource names and concrete steps.");

            return sb.ToString();
        }
    }
}