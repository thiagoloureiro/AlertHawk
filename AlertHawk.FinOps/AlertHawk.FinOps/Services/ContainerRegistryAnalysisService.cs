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
    public class ContainerRegistryAnalysisService : IResourceAnalysisService
    {
        private readonly ClientSecretCredential _credential;

        public ContainerRegistryAnalysisService(ClientSecretCredential credential)
        {
            _credential = credential;
        }

        public async Task AnalyzeAsync(SubscriptionResource subscription)
        {
            Console.WriteLine("\n=== Checking Container Registries ===");

            try
            {
                var metricsClient = new MetricsQueryClient(_credential);
                var resourceGroups = subscription.GetResourceGroups();

                await foreach (var rg in resourceGroups)
                {
                    var registries = rg.GetGenericResourcesAsync(
                        filter: "resourceType eq 'Microsoft.ContainerRegistry/registries'");

                    await foreach (var registry in registries)
                    {
                        Console.WriteLine($"\n📦 Container Registry: {registry.Data.Name}");
                        Console.WriteLine($"  Resource Group: {registry.Data.Id.ResourceGroupName}");
                        Console.WriteLine($"  Location: {registry.Data.Location}");

                        if (registry.Data.Sku != null)
                        {
                            Console.WriteLine($"  📦 SKU:");
                            Console.WriteLine($"    - Tier: {registry.Data.Sku.Tier ?? "N/A"}");
                            Console.WriteLine($"    - Name: {registry.Data.Sku.Name ?? "N/A"}");
                        }

                        if (registry.Data.Properties != null)
                        {
                            var props = JsonSerializer.Deserialize<JsonElement>(registry.Data.Properties.ToString());

                            if (props.TryGetProperty("adminUserEnabled", out var admin) && admin.ValueKind == JsonValueKind.True)
                            {
                                Console.WriteLine($"  ⚠️  Admin user: enabled (prefer managed identity or token auth where possible)");
                            }

                            if (props.TryGetProperty("publicNetworkAccess", out var pub))
                            {
                                Console.WriteLine($"  Public network access: {pub.GetString() ?? "N/A"}");
                            }

                            if (props.TryGetProperty("networkRuleBypassOptions", out var bypass))
                            {
                                Console.WriteLine($"  Network rule bypass: {bypass.GetString() ?? "N/A"}");
                            }
                        }

                        var endTime = DateTimeOffset.UtcNow;
                        var startTime = endTime.AddDays(-7);

                        try
                        {
                            Console.WriteLine($"  📈 Metrics (Last 7 Days):");

                            var metricsResponse = await metricsClient.QueryResourceWithRetryAsync(
                                registry.Id.ToString(),
                                new[]
                                {
                                    "StorageUsed",
                                    "TotalPullCount",
                                    "TotalPushCount",
                                    "SuccessfulPullCount",
                                    "SuccessfulPushCount"
                                },
                                new MetricsQueryOptions
                                {
                                    TimeRange = new QueryTimeRange(startTime, endTime),
                                    Granularity = TimeSpan.FromHours(1),
                                    Aggregations =
                                    {
                                        MetricAggregationType.Average,
                                        MetricAggregationType.Maximum,
                                        MetricAggregationType.Total
                                    }
                                });

                            foreach (var metric in metricsResponse.Value.Metrics)
                            {
                                var timeSeries = metric.TimeSeries.SelectMany(ts => ts.Values).ToList();

                                var avgValue = timeSeries
                                    .Where(v => v.Average.HasValue)
                                    .Select(v => v.Average!.Value)
                                    .DefaultIfEmpty(0)
                                    .Average();

                                var maxValue = timeSeries
                                    .Where(v => v.Maximum.HasValue)
                                    .Select(v => v.Maximum!.Value)
                                    .DefaultIfEmpty(0)
                                    .Max();

                                var totalValue = timeSeries
                                    .Where(v => v.Total.HasValue)
                                    .Select(v => v.Total!.Value)
                                    .DefaultIfEmpty(0)
                                    .Sum();

                                switch (metric.Name)
                                {
                                    case "StorageUsed":
                                        Console.WriteLine(
                                            $"    - Storage used: Avg = {FormatBytes(avgValue)}, Max = {FormatBytes(maxValue)}");
                                        break;
                                    case "TotalPullCount":
                                        Console.WriteLine($"    - Total pulls: {totalValue:F0}");
                                        break;
                                    case "TotalPushCount":
                                        Console.WriteLine($"    - Total pushes: {totalValue:F0}");
                                        break;
                                    case "SuccessfulPullCount":
                                        Console.WriteLine($"    - Successful pulls: {totalValue:F0}");
                                        break;
                                    case "SuccessfulPushCount":
                                        Console.WriteLine($"    - Successful pushes: {totalValue:F0}");
                                        break;
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            SentrySdk.CaptureException(ex);
                            Console.WriteLine($"  ❌ Could not fetch metrics: {ex.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                SentrySdk.CaptureException(ex);
                Console.WriteLine($"Error checking container registries: {ex.Message}");
            }
        }

        private static string FormatBytes(double bytes)
        {
            const double k = 1024d;
            if (bytes >= k * k * k)
            {
                return $"{bytes / (k * k * k):F2} GiB";
            }

            if (bytes >= k * k)
            {
                return $"{bytes / (k * k):F2} MiB";
            }

            if (bytes >= k)
            {
                return $"{bytes / k:F2} KiB";
            }

            return $"{bytes:F0} B";
        }
    }
}
