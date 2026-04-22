using Azure.Monitor.Query;
using Azure.Monitor.Query.Models;
using Azure.ResourceManager.Resources;
using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace FinOpsToolSample.Services
{
    public class VirtualMachineAnalysisService : IResourceAnalysisService
    {
        private readonly MetricsQueryClient _metricsClient;

        public VirtualMachineAnalysisService(MetricsQueryClient metricsClient)
        {
            _metricsClient = metricsClient;
        }

        public async Task AnalyzeAsync(SubscriptionResource subscription)
        {
            Console.WriteLine("\n=== Checking Virtual Machines ===");

            try
            {
                var resourceGroups = subscription.GetResourceGroups();

                await foreach (var rg in resourceGroups)
                {
                    var vms = rg.GetGenericResourcesAsync(
                        filter: "resourceType eq 'Microsoft.Compute/virtualMachines'"
                    );

                    await foreach (var vm in vms)
                    {
                        Console.WriteLine($"\n🖥️  VM: {vm.Data.Name}");
                        Console.WriteLine($"  Resource Group: {vm.Data.Id.ResourceGroupName}");
                        Console.WriteLine($"  Location: {vm.Data.Location}");

                        if (vm.Data.Properties != null)
                        {
                            var props = JsonSerializer.Deserialize<JsonElement>(vm.Data.Properties.ToString());

                            if (props.TryGetProperty("hardwareProfile", out var hwProfile))
                            {
                                if (hwProfile.TryGetProperty("vmSize", out var vmSize))
                                {
                                    Console.WriteLine($"  Size: {vmSize.GetString()}");
                                }
                            }

                            if (props.TryGetProperty("storageProfile", out var storageProfile))
                            {
                                if (storageProfile.TryGetProperty("osDisk", out var osDisk))
                                {
                                    if (osDisk.TryGetProperty("diskSizeGB", out var diskSize))
                                    {
                                        Console.WriteLine($"  OS Disk Size: {diskSize.GetInt32()} GB");
                                    }
                                }
                            }
                        }

                        var endTime = DateTimeOffset.UtcNow;
                        var startTime = endTime.AddDays(-7);

                        try
                        {
                            Console.WriteLine($"  📈 Performance Metrics (Last 7 Days):");

                            var metricsResponse = await _metricsClient.QueryResourceWithRetryAsync(
                                vm.Id.ToString(),
                                new[] { "Percentage CPU", "Network In Total", "Network Out Total", "Disk Read Bytes", "Disk Write Bytes" },
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

                                if (metric.Name == "Percentage CPU")
                                {
                                    Console.WriteLine($"    - CPU Usage: Avg = {avgValue:F2}%, Max = {maxValue:F2}%");
                                }
                                else if (metric.Name.Contains("Network"))
                                {
                                    Console.WriteLine($"    - {metric.Name}: Avg = {avgValue / 1024 / 1024:F2} MB, Max = {maxValue / 1024 / 1024:F2} MB");
                                }
                                else if (metric.Name.Contains("Disk"))
                                {
                                    Console.WriteLine($"    - {metric.Name}: Avg = {avgValue / 1024 / 1024:F2} MB, Max = {maxValue / 1024 / 1024:F2} MB");
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
                Console.WriteLine($"Error checking VMs: {ex.Message}");
            }
        }
    }
}
