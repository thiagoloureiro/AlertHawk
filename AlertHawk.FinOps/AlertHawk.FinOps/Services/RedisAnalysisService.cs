using Azure.Monitor.Query;
using Azure.Monitor.Query.Models;
using Azure.ResourceManager.Resources;
using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace FinOpsToolSample.Services
{
    public class RedisAnalysisService : IResourceAnalysisService
    {
        private readonly MetricsQueryClient _metricsClient;

        public RedisAnalysisService(MetricsQueryClient metricsClient)
        {
            _metricsClient = metricsClient;
        }

        public async Task AnalyzeAsync(SubscriptionResource subscription)
        {
            Console.WriteLine("\n=== Checking Redis Caches ===");

            try
            {
                var resourceGroups = subscription.GetResourceGroups();

                await foreach (var rg in resourceGroups)
                {
                    var redisCaches = rg.GetGenericResourcesAsync(
                        filter: "resourceType eq 'Microsoft.Cache/Redis'"
                    );

                    await foreach (var cache in redisCaches)
                    {
                        Console.WriteLine($"\n🔴 Redis Cache: {cache.Data.Name}");
                        Console.WriteLine($"  Resource Group: {cache.Data.Id.ResourceGroupName}");
                        Console.WriteLine($"  Location: {cache.Data.Location}");

                        if (cache.Data.Sku != null)
                        {
                            Console.WriteLine($"  📦 SKU Details:");
                            Console.WriteLine($"    - Tier: {cache.Data.Sku.Tier ?? "N/A"}");
                            Console.WriteLine($"    - Name: {cache.Data.Sku.Name ?? "N/A"}");
                            
                            if (cache.Data.Sku.Capacity.HasValue)
                            {
                                Console.WriteLine($"    - Capacity: {cache.Data.Sku.Capacity.Value}");
                            }
                        }

                        if (cache.Data.Properties != null)
                        {
                            var props = JsonSerializer.Deserialize<JsonElement>(cache.Data.Properties.ToString());

                            if (props.TryGetProperty("redisVersion", out var version))
                            {
                                Console.WriteLine($"  Redis Version: {version.GetString()}");
                            }

                            if (props.TryGetProperty("enableNonSslPort", out var nonSsl) && nonSsl.GetBoolean())
                            {
                                Console.WriteLine($"  ⚠️  Non-SSL Port: Enabled (consider disabling for security)");
                            }

                            if (props.TryGetProperty("sku", out var skuProps))
                            {
                                if (skuProps.TryGetProperty("family", out var family))
                                {
                                    Console.WriteLine($"  Family: {family.GetString()}");
                                }
                            }
                        }

                        var endTime = DateTimeOffset.UtcNow;
                        var startTime = endTime.AddDays(-7);

                        try
                        {
                            Console.WriteLine($"  📈 Performance Metrics (Last 7 Days):");

                            var metricsResponse = await _metricsClient.QueryResourceWithRetryAsync(
                                cache.Id.ToString(),
                                new[] { "percentProcessorTime", "usedmemorypercentage" },
                                new MetricsQueryOptions
                                {
                                    TimeRange = new QueryTimeRange(startTime, endTime),
                                    Granularity = TimeSpan.FromHours(1),
                                    Aggregations = { MetricAggregationType.Average, MetricAggregationType.Maximum }
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

                                var maxValue = timeSeries
                                    .Where(v => v.Maximum.HasValue)
                                    .Select(v => v.Maximum.Value)
                                    .DefaultIfEmpty(0)
                                    .Max();

                                var metricDisplay = metric.Name switch
                                {
                                    "percentProcessorTime" => "CPU Usage",
                                    "usedmemorypercentage" => "Memory Usage",
                                    _ => metric.Name
                                };

                                Console.WriteLine($"    - {metricDisplay}: Avg = {avgValue:F2}%, Max = {maxValue:F2}%");

                                if (metric.Name == "percentProcessorTime" && maxValue > 90)
                                {
                                    Console.WriteLine($"    ⚠️  High CPU usage detected");
                                }

                                if (metric.Name == "usedmemorypercentage" && maxValue > 90)
                                {
                                    Console.WriteLine($"    ⚠️  High memory usage detected");
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
                Console.WriteLine($"Error checking Redis Caches: {ex.Message}");
            }
        }
    }
}
