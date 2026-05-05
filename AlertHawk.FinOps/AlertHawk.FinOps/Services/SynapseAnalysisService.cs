using Azure.Monitor.Query;
using Azure.Monitor.Query.Models;
using Azure.ResourceManager.Resources;
using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace FinOpsToolSample.Services
{
    public class SynapseAnalysisService : IResourceAnalysisService
    {
        private readonly MetricsQueryClient _metricsClient;

        public SynapseAnalysisService(MetricsQueryClient metricsClient)
        {
            _metricsClient = metricsClient;
        }

        public async Task AnalyzeAsync(SubscriptionResource subscription)
        {
            Console.WriteLine("\n=== Checking Azure Synapse ===");

            try
            {
                var resourceGroups = subscription.GetResourceGroups();

                await foreach (var rg in resourceGroups)
                {
                    var workspaces = rg.GetGenericResourcesAsync(
                        filter: "resourceType eq 'Microsoft.Synapse/workspaces'");

                    await foreach (var ws in workspaces)
                    {
                        Console.WriteLine($"\n🧩 Synapse Workspace: {ws.Data.Name}");
                        Console.WriteLine($"  Resource Group: {ws.Data.Id.ResourceGroupName}");
                        Console.WriteLine($"  Location: {ws.Data.Location}");

                        if (ws.Data.Sku != null)
                        {
                            Console.WriteLine($"  📦 SKU:");
                            Console.WriteLine($"    - Tier: {ws.Data.Sku.Tier ?? "N/A"}");
                            Console.WriteLine($"    - Name: {ws.Data.Sku.Name ?? "N/A"}");
                        }

                        var endTime = DateTimeOffset.UtcNow;
                        var startTime = endTime.AddDays(-7);

                        try
                        {
                            Console.WriteLine($"  📈 Built-in SQL pool activity (Last 7 Days):");
                            var wsMetrics = await _metricsClient.QueryResourceWithRetryAsync(
                                ws.Id.ToString(),
                                new[] { "BuiltinSqlPoolDataProcessedBytes" },
                                new MetricsQueryOptions
                                {
                                    TimeRange = new QueryTimeRange(startTime, endTime),
                                    Granularity = TimeSpan.FromHours(1),
                                    Aggregations =
                                    {
                                        MetricAggregationType.Total,
                                        MetricAggregationType.Average
                                    }
                                });

                            foreach (var metric in wsMetrics.Value.Metrics)
                            {
                                var timeSeries = metric.TimeSeries.SelectMany(ts => ts.Values).ToList();
                                var avg = timeSeries.Where(v => v.Average.HasValue).Select(v => v.Average!.Value)
                                    .DefaultIfEmpty(0).Average();
                                var total = timeSeries.Where(v => v.Total.HasValue).Select(v => v.Total!.Value)
                                    .DefaultIfEmpty(0).Sum();
                                Console.WriteLine(
                                    $"    - Data processed: Total = {FormatBytes(total)}, Hourly avg ≈ {FormatBytes(avg)}");
                            }
                        }
                        catch (Exception ex)
                        {
                            SentrySdk.CaptureException(ex);
                            Console.WriteLine($"  ❌ Could not fetch workspace metrics: {ex.Message}");
                        }
                    }

                    var sqlPools = rg.GetGenericResourcesAsync(
                        filter: "resourceType eq 'Microsoft.Synapse/workspaces/sqlPools'");

                    await foreach (var pool in sqlPools)
                    {
                        Console.WriteLine($"\n📊 Synapse Dedicated SQL Pool: {pool.Data.Name}");
                        Console.WriteLine($"  Resource Group: {pool.Data.Id.ResourceGroupName}");
                        Console.WriteLine($"  Location: {pool.Data.Location}");

                        if (pool.Data.Sku != null)
                        {
                            Console.WriteLine($"  📦 SKU:");
                            Console.WriteLine($"    - Tier: {pool.Data.Sku.Tier ?? "N/A"}");
                            Console.WriteLine($"    - Name: {pool.Data.Sku.Name ?? "N/A"}");
                            if (pool.Data.Sku.Capacity.HasValue)
                            {
                                Console.WriteLine($"    - Capacity: {pool.Data.Sku.Capacity.Value} DWUs");
                            }
                        }

                        if (pool.Data.Properties != null)
                        {
                            var props = JsonSerializer.Deserialize<JsonElement>(pool.Data.Properties.ToString());
                            if (props.TryGetProperty("status", out var status))
                            {
                                Console.WriteLine($"  Status: {status.GetString()}");
                            }
                        }

                        var endTime = DateTimeOffset.UtcNow;
                        var startTime = endTime.AddDays(-7);

                        try
                        {
                            Console.WriteLine($"  📈 Pool metrics (Last 7 Days, Azure Monitor sqlPools):");
                            var poolMetrics = await _metricsClient.QueryResourceWithRetryAsync(
                                pool.Id.ToString(),
                                new[] { "DWUUsedPercent", "CPUPercent", "MemoryUsedPercent" },
                                new MetricsQueryOptions
                                {
                                    TimeRange = new QueryTimeRange(startTime, endTime),
                                    Granularity = TimeSpan.FromHours(1),
                                    Aggregations =
                                    {
                                        MetricAggregationType.Average,
                                        MetricAggregationType.Maximum
                                    }
                                });

                            foreach (var metric in poolMetrics.Value.Metrics)
                            {
                                var timeSeries = metric.TimeSeries.SelectMany(ts => ts.Values).ToList();
                                var avgValue = timeSeries.Where(v => v.Average.HasValue).Select(v => v.Average!.Value)
                                    .DefaultIfEmpty(0).Average();
                                var maxValue = timeSeries.Where(v => v.Maximum.HasValue).Select(v => v.Maximum!.Value)
                                    .DefaultIfEmpty(0).Max();

                                var label = metric.Name switch
                                {
                                    "DWUUsedPercent" => "DWU used",
                                    "CPUPercent" => "CPU",
                                    "MemoryUsedPercent" => "Memory",
                                    _ => metric.Name
                                };

                                Console.WriteLine($"    - {label}: Avg = {avgValue:F2}%, Max = {maxValue:F2}%");
                            }
                        }
                        catch (Exception ex)
                        {
                            SentrySdk.CaptureException(ex);
                            Console.WriteLine($"  ❌ Could not fetch pool metrics: {ex.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                SentrySdk.CaptureException(ex);
                Console.WriteLine($"Error checking Synapse: {ex.Message}");
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
