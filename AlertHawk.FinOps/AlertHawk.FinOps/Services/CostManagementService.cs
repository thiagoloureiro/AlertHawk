using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.Resources;
using FinOpsToolSample.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace FinOpsToolSample.Services
{
    public class CostManagementService : ICostAnalysisService
    {
        private readonly ClientSecretCredential _credential;

        public CostManagementService(ClientSecretCredential credential)
        {
            _credential = credential;
        }

        public async Task<(decimal totalCost, Dictionary<string, decimal> byResourceGroup, List<ServiceCostDetail> byService)> AnalyzeAsync(SubscriptionResource subscription, ArmClient armClient)
        {
            Console.WriteLine("=== Fetching Cost Data ===");

            try
            {
                var subscriptionData = await AzureThrottledRequestRetry.ExecuteAsync(
                    () => subscription.GetAsync());

                Console.WriteLine($"Subscription: {subscriptionData.Value.Data.DisplayName}");
                Console.WriteLine($"Subscription ID: {subscriptionData.Value.Data.SubscriptionId}");
                Console.WriteLine();

                var subscriptionId = subscriptionData.Value.Data.SubscriptionId;

                var tokenRequestContext = new Azure.Core.TokenRequestContext(
                    new[] { "https://management.azure.com/.default" }
                );
                var token = await AzureThrottledRequestRetry.ExecuteAsync(
                    async () => await _credential.GetTokenAsync(tokenRequestContext, default));

                var queryPayload = new
                {
                    type = "ActualCost",
                    timeframe = "MonthToDate",
                    dataset = new
                    {
                        granularity = "Daily",
                        aggregation = new Dictionary<string, object>
                        {
                            ["totalCost"] = new { name = "PreTaxCost", function = "Sum" }
                        },
                        grouping = new[]
                        {
                            new { type = "Dimension", name = "ResourceGroupName" },
                            new { type = "Dimension", name = "ServiceName" }
                        }
                    }
                };

                var jsonPayload = JsonSerializer.Serialize(queryPayload);

                using var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token.Token);

                var url = $"https://management.azure.com/subscriptions/{subscriptionId}/providers/Microsoft.CostManagement/query?api-version=2023-11-01";
                using var response = await AzureThrottledRequestRetry.SendPostWithRetryAsync(
                    httpClient,
                    url,
                    () => new StringContent(jsonPayload, Encoding.UTF8, "application/json"));

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    throw new Exception($"API Error: {response.StatusCode} - {error}");
                }

                var resultJson = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<JsonElement>(resultJson);

                var rows = result.GetProperty("properties").GetProperty("rows");

                Console.WriteLine($"\n📊 Cost Analysis Results (Month to Date):");
                Console.WriteLine($"Total rows: {rows.GetArrayLength()}");
                Console.WriteLine();

                var (totalCost, costsByResourceGroup, costsByService) =
                    CostManagementQueryResultParser.ParseCostRows(rows);

                Console.WriteLine("💰 Top Costs by Resource Group:");
                foreach (var kvp in costsByResourceGroup.OrderByDescending(x => x.Value).Take(10))
                {
                    Console.WriteLine($"  {kvp.Key}: ${kvp.Value:F2}");
                }

                Console.WriteLine("\n💰 Top Costs by Service:");
                var serviceAggregated = costsByService
                    .GroupBy(s => s.ServiceName)
                    .Select(g => new { ServiceName = g.Key, Cost = g.Sum(x => x.Cost) })
                    .OrderByDescending(x => x.Cost)
                    .Take(10);

                foreach (var svc in serviceAggregated)
                {
                    Console.WriteLine($"  {svc.ServiceName}: ${svc.Cost:F2}");
                }

                Console.WriteLine($"\n💵 Total Cost (Month to Date): ${totalCost:F2}");

                return (totalCost, costsByResourceGroup, costsByService);
            }
            catch (Exception ex)
            {
                SentrySdk.CaptureException(ex);
                Console.WriteLine($"Error fetching cost data: {ex.Message}");
                Console.WriteLine("\nNote: Ensure your service principal has 'Cost Management Reader' role.");
                return (0, new Dictionary<string, decimal>(), new List<ServiceCostDetail>());
            }
        }
    }
}
