using Azure.Identity;
using Azure.Monitor.Query;
using Azure.Monitor.Query.Models;
using Azure.ResourceManager.Resources;
using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace FinOpsToolSample.Services
{
    public class AppServiceAnalysisService : IResourceAnalysisService
    {
        private readonly ClientSecretCredential _credential;

        public AppServiceAnalysisService(ClientSecretCredential credential)
        {
            _credential = credential;
        }

        public async Task AnalyzeAsync(SubscriptionResource subscription)
        {
            Console.WriteLine("\n=== Checking App Services ===");

            try
            {
                var metricsClient = new MetricsQueryClient(_credential);
                var resourceGroups = subscription.GetResourceGroups();

                await foreach (var rg in resourceGroups)
                {
                    var webApps = rg.GetGenericResourcesAsync(
                        filter: "resourceType eq 'Microsoft.Web/sites'"
                    );

                    await foreach (var app in webApps)
                    {
                        Console.WriteLine($"\n🌐 App Service: {app.Data.Name}");
                        Console.WriteLine($"  Resource Group: {app.Data.Id.ResourceGroupName}");
                        Console.WriteLine($"  Location: {app.Data.Location}");

                        if (app.Data.Properties != null)
                        {
                            var props = JsonSerializer.Deserialize<JsonElement>(app.Data.Properties.ToString());

                            if (props.TryGetProperty("state", out var state))
                            {
                                Console.WriteLine($"  State: {state.GetString()}");
                            }

                            if (props.TryGetProperty("defaultHostName", out var hostname))
                            {
                                Console.WriteLine($"  Hostname: {hostname.GetString()}");
                            }

                            if (props.TryGetProperty("serverFarmId", out var farmId))
                            {
                                var planName = farmId.GetString()?.Split('/').LastOrDefault();
                                Console.WriteLine($"  App Service Plan: {planName}");
                            }
                        }

                        var endTime = DateTimeOffset.UtcNow;
                        var startTime = endTime.AddDays(-7);

                        try
                        {
                            Console.WriteLine($"  📈 Performance Metrics (Last 7 Days):");

                            // Query only metrics available across all App Service types
                            var metricsResponse = await metricsClient.QueryResourceAsync(
                                app.Id.ToString(),
                                new[] { "Requests", "MemoryWorkingSet", "Http5xx", "AverageResponseTime" },
                                new MetricsQueryOptions
                                {
                                    TimeRange = new QueryTimeRange(startTime, endTime),
                                    Granularity = TimeSpan.FromHours(1),
                                    Aggregations = { MetricAggregationType.Average, MetricAggregationType.Total }
                                }
                            );

                            foreach (var metric in metricsResponse.Value.Metrics)
                            {
                                var timeSeries = metric.TimeSeries.SelectMany(ts => ts.Values).ToList();

                                var avgValue = timeSeries
                                    .Where(v => v.Average.HasValue)
                                    .Select(v => v.Average.Value)
                                    .DefaultIfEmpty(0)
                                    .Average();

                                var totalValue = timeSeries
                                    .Where(v => v.Total.HasValue)
                                    .Select(v => v.Total.Value)
                                    .DefaultIfEmpty(0)
                                    .Sum();

                                if (metric.Name == "Requests")
                                {
                                    Console.WriteLine($"    - Requests: Total = {totalValue:F0}");
                                }
                                else if (metric.Name == "MemoryWorkingSet")
                                {
                                    Console.WriteLine($"    - Memory: Avg = {avgValue / 1024 / 1024:F2} MB");
                                }
                                else if (metric.Name == "Http5xx")
                                {
                                    Console.WriteLine($"    - HTTP 5xx Errors: Total = {totalValue:F0}");
                                }
                                else if (metric.Name == "AverageResponseTime")
                                {
                                    Console.WriteLine($"    - Response Time: Avg = {avgValue:F2} seconds");
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"  ❌ Could not fetch metrics: {ex.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error checking App Services: {ex.Message}");
            }
        }
    }
}