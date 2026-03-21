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
                var subscriptionData = await subscription.GetAsync();

                Console.WriteLine($"Subscription: {subscriptionData.Value.Data.DisplayName}");
                Console.WriteLine($"Subscription ID: {subscriptionData.Value.Data.SubscriptionId}");
                Console.WriteLine();

                var subscriptionId = subscriptionData.Value.Data.SubscriptionId;

                var tokenRequestContext = new Azure.Core.TokenRequestContext(
                    new[] { "https://management.azure.com/.default" }
                );
                var token = await _credential.GetTokenAsync(tokenRequestContext, default);

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
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                using var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token.Token);

                var url = $"https://management.azure.com/subscriptions/{subscriptionId}/providers/Microsoft.CostManagement/query?api-version=2023-11-01";
                var response = await httpClient.PostAsync(url, content);

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

                var costsByResourceGroup = new Dictionary<string, decimal>();
                var costsByService = new List<ServiceCostDetail>();

                foreach (var row in rows.EnumerateArray())
                {
                    var values = row.EnumerateArray().ToList();
                    var cost = values[0].GetDecimal();
                    var resourceGroup = values.Count > 2 ? values[2].GetString() ?? "Unknown" : "Unknown";
                    var serviceName = values.Count > 3 ? values[3].GetString() ?? "Unknown" : "Unknown";

                    if (!costsByResourceGroup.ContainsKey(resourceGroup))
                        costsByResourceGroup[resourceGroup] = 0;
                    costsByResourceGroup[resourceGroup] += cost;

                    costsByService.Add(new ServiceCostDetail
                    {
                        ServiceName = serviceName,
                        ResourceGroup = resourceGroup,
                        Cost = cost
                    });
                }

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

                var totalCost = costsByResourceGroup.Values.Sum();
                Console.WriteLine($"\n💵 Total Cost (Month to Date): ${totalCost:F2}");

                return (totalCost, costsByResourceGroup, costsByService);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching cost data: {ex.Message}");
                Console.WriteLine("\nNote: Ensure your service principal has 'Cost Management Reader' role.");
                return (0, new Dictionary<string, decimal>(), new List<ServiceCostDetail>());
            }
        }
    }
}
