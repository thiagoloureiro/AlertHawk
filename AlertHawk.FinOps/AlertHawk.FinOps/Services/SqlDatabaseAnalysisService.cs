using Azure.Identity;
using Azure.Monitor.Query;
using Azure.Monitor.Query.Models;
using Azure.ResourceManager.Resources;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace FinOpsToolSample.Services
{
    public class SqlDatabaseAnalysisService : IResourceAnalysisService
    {
        private readonly ClientSecretCredential _credential;

        public SqlDatabaseAnalysisService(ClientSecretCredential credential)
        {
            _credential = credential;
        }

        public async Task AnalyzeAsync(SubscriptionResource subscription)
        {
            Console.WriteLine("\n=== Checking Database Usage ===");

            try
            {
                var metricsClient = new MetricsQueryClient(_credential);
                var resourceGroups = subscription.GetResourceGroups();

                await foreach (var rg in resourceGroups)
                {
                    var resources = rg.GetGenericResourcesAsync(
                        filter: "resourceType eq 'Microsoft.Sql/servers/databases'"
                    );

                    await foreach (var db in resources)
                    {
                        if (db.Data.Name.ToLower() == "master") continue;

                        Console.WriteLine($"\n📊 Checking database: {db.Data.Name}");
                        Console.WriteLine($"  Resource Group: {db.Data.Id.ResourceGroupName}");
                        Console.WriteLine($"  Location: {db.Data.Location}");

                        // Check if database is in an elastic pool
                        var isElasticPool = db.Data.Sku?.Name?.Equals("ElasticPool", StringComparison.OrdinalIgnoreCase) ?? false;

                        if (db.Data.Sku != null)
                        {
                            Console.WriteLine($"  📦 SKU Details:");
                            Console.WriteLine($"    - Tier: {db.Data.Sku.Tier ?? "N/A"}");
                            Console.WriteLine($"    - Name: {db.Data.Sku.Name ?? "N/A"}");

                            if (isElasticPool)
                            {
                                Console.WriteLine($"    - Type: Elastic Pool Database");
                            }

                            if (db.Data.Sku.Capacity.HasValue)
                            {
                                var capacity = db.Data.Sku.Capacity.Value;
                                var tier = db.Data.Sku.Tier?.ToLower() ?? "";
                                var skuName = db.Data.Sku.Name?.ToLower() ?? "";

                                if (tier.Contains("datawarehouse") || skuName.Contains("datawarehouse") || skuName.StartsWith("dw"))
                                {
                                    Console.WriteLine($"    - Capacity: {capacity} DWUs");
                                }
                                else if (tier.Contains("basic") || tier.Contains("standard") || tier.Contains("premium"))
                                {
                                    Console.WriteLine($"    - Capacity: {capacity} DTUs");
                                }
                                else if (tier.Contains("general") || tier.Contains("business") || tier.Contains("hyperscale"))
                                {
                                    Console.WriteLine($"    - Capacity: {capacity} vCores");
                                }
                                else
                                {
                                    Console.WriteLine($"    - Capacity: {capacity}");
                                }
                            }

                            if (db.Data.Sku.Size != null)
                            {
                                Console.WriteLine($"    - Size: {db.Data.Sku.Size}");
                            }
                        }

                        var endTime = DateTimeOffset.UtcNow;
                        var startTime = endTime.AddDays(-7);

                        try
                        {
                            Console.WriteLine($"  📈 Performance Metrics (Last 7 Days):");

                            // Determine which metrics to query based on tier and pool status
                            var tier = db.Data.Sku?.Tier?.ToLower() ?? "";
                            var skuName = db.Data.Sku?.Name?.ToLower() ?? "";
                            string[] metricsToQuery;

                            if (isElasticPool)
                            {
                                // Elastic Pool databases use different metrics
                                Console.WriteLine($"  → Detected Elastic Pool Database, using available metrics");
                                metricsToQuery = new[] { "cpu_percent", "physical_data_read_percent", "log_write_percent" };
                            }
                            else if (tier.Contains("datawarehouse") || skuName.Contains("datawarehouse") || skuName.StartsWith("dw"))
                            {
                                // Data Warehouse (Synapse) uses DWU metrics
                                Console.WriteLine($"  → Detected Data Warehouse, using DWU metrics");
                                metricsToQuery = new[] { "dwu_consumption_percent", "cpu_percent", "memory_usage_percent" };
                            }
                            else if (tier.Contains("basic") || tier.Contains("standard") || tier.Contains("premium"))
                            {
                                // DTU-based databases
                                Console.WriteLine($"  → Detected DTU-based database");
                                metricsToQuery = new[] { "dtu_consumption_percent", "cpu_percent", "storage_percent" };
                            }
                            else
                            {
                                // vCore-based databases (GeneralPurpose, BusinessCritical, Hyperscale)
                                Console.WriteLine($"  → Detected vCore-based database");
                                metricsToQuery = new[] { "cpu_percent", "storage_percent" };
                            }

                            var metricsResponse = await metricsClient.QueryResourceAsync(
                                db.Id.ToString(),
                                metricsToQuery,
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
                                    "cpu_percent" => "CPU Usage",
                                    "dtu_consumption_percent" => "DTU Consumption",
                                    "dwu_consumption_percent" => "DWU Consumption",
                                    "storage_percent" => "Storage Usage",
                                    "memory_usage_percent" => "Memory Usage",
                                    "physical_data_read_percent" => "Physical Data Read",
                                    "log_write_percent" => "Log Write",
                                    _ => metric.Name
                                };

                                Console.WriteLine($"    - {metricDisplay}: Avg = {avgValue:F2}%, Max = {maxValue:F2}%");
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
                Console.WriteLine($"Error checking database usage: {ex.Message}");
            }
        }
    }
}
