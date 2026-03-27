using Azure.Identity;
using Azure.Monitor.Query;
using Azure.Monitor.Query.Models;
using Azure.ResourceManager.AppService;
using Azure.ResourceManager.Resources;
using FinOpsToolSample.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace FinOpsToolSample.Services
{
    public class DataCollectionService
    {
        private readonly AzureResourceData _data = new();

        public AzureResourceData GetCollectedData() => _data;

        public void SetSubscriptionInfo(string name, string id)
        {
            _data.SubscriptionName = name;
            _data.SubscriptionId = id;
        }

        public void SetCostData(decimal totalCost, Dictionary<string, decimal> byResourceGroup, List<ServiceCostDetail> byService)
        {
            _data.TotalMonthlyCost = totalCost;
            _data.CostsByResourceGroup = byResourceGroup;
            _data.CostsByService = byService;
        }

        public void AddResource(ResourceInfo resource)
        {
            _data.Resources.Add(resource);
        }

        public async Task CollectAppServicePlans(SubscriptionResource subscription, ClientSecretCredential credential)
        {
            try
            {
                var metricsClient = new MetricsQueryClient(credential);
                var resourceGroups = subscription.GetResourceGroups();

                await foreach (var rg in resourceGroups)
                {
                    // Collect App Service Plans
                    var plans = rg.GetGenericResourcesAsync(
                        filter: "resourceType eq 'Microsoft.Web/serverfarms'"
                    );

                    await foreach (var plan in plans)
                    {
                        var planResource = new ResourceInfo
                        {
                            Type = "App Service Plan",
                            Name = plan.Data.Name,
                            ResourceGroup = plan.Data.Id.ResourceGroupName ?? "Unknown",
                            Location = plan.Data.Location.ToString()
                        };

                        if (plan.Data.Sku != null)
                        {
                            DataCollectionAppServicePlanSku.ApplyToResource(
                                planResource,
                                plan.Data.Sku.Name,
                                plan.Data.Sku.Tier,
                                plan.Data.Sku.Capacity);
                        }

                        AddResource(planResource);
                    }

                    // Collect App Services (Web Apps) with metrics
                    var webApps = rg.GetGenericResourcesAsync(
                        filter: "resourceType eq 'Microsoft.Web/sites'"
                    );

                    await foreach (var app in webApps)
                    {
                        var appResource = new ResourceInfo
                        {
                            Type = "App Service",
                            Name = app.Data.Name,
                            ResourceGroup = app.Data.Id.ResourceGroupName ?? "Unknown",
                            Location = app.Data.Location.ToString()
                        };

                        if (app.Data.Properties != null)
                        {
                            var props = JsonSerializer.Deserialize<JsonElement>(app.Data.Properties.ToString());
                            DataCollectionWebAppJsonProperties.ApplySitePropertiesFromJson(props, appResource);
                        }

                        // Fetch App Service metrics
                        try
                        {
                            var endTime = DateTimeOffset.UtcNow;
                            var startTime = endTime.AddDays(-7);

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
                                var (avgValue, totalValue) = AppServiceAnalysisMetrics.SummarizeTimeSeriesPoints(
                                    timeSeries.Select(v => (v.Average, v.Total)));
                                DataCollectionAppWebMonitoringMetrics.ApplyMetric(
                                    appResource,
                                    metric.Name,
                                    avgValue,
                                    totalValue);
                            }
                        }
                        catch (Exception ex)
                        {
                            SentrySdk.CaptureException(ex);
                            Console.WriteLine($"Could not fetch metrics for App Service {app.Data.Name}: {ex.Message}");
                        }

                        AddResource(appResource);
                    }
                }
            }
            catch (Exception ex)
            {
                SentrySdk.CaptureException(ex);
                Console.WriteLine($"Error collecting App Service Plans: {ex.Message}");
            }
        }

        public async Task CollectSqlDatabases(SubscriptionResource subscription, ClientSecretCredential credential)
        {
            try
            {
                var metricsClient = new MetricsQueryClient(credential);
                var resourceGroups = subscription.GetResourceGroups();

                await foreach (var rg in resourceGroups)
                {
                    var databases = rg.GetGenericResourcesAsync(
                        filter: "resourceType eq 'Microsoft.Sql/servers/databases'"
                    );

                    await foreach (var db in databases)
                    {
                        if (db.Data.Name.ToLower() == "master") continue;

                        var resource = new ResourceInfo
                        {
                            Type = "SQL Database",
                            Name = db.Data.Name,
                            ResourceGroup = db.Data.Id.ResourceGroupName ?? "Unknown",
                            Location = db.Data.Location.ToString()
                        };

                        if (db.Data.Sku != null)
                        {
                            resource.Properties["Tier"] = db.Data.Sku.Tier ?? "Unknown";
                            resource.Properties["SKU"] = db.Data.Sku.Name ?? "Unknown";

                            if (db.Data.Sku.Capacity.HasValue)
                            {
                                var tier = db.Data.Sku.Tier?.ToLower() ?? "";
                                var skuName = db.Data.Sku.Name?.ToLower() ?? "";

                                if (tier.Contains("datawarehouse") || skuName.Contains("datawarehouse") || skuName == "dw")
                                {
                                    resource.Properties["Capacity"] = $"{db.Data.Sku.Capacity.Value} DWUs";
                                }
                                else if (tier.Contains("basic") || tier.Contains("standard") || tier.Contains("premium"))
                                {
                                    resource.Properties["Capacity"] = $"{db.Data.Sku.Capacity.Value} DTUs";
                                }
                                else if (tier.Contains("general") || tier.Contains("business") || tier.Contains("hyperscale"))
                                {
                                    resource.Properties["Capacity"] = $"{db.Data.Sku.Capacity.Value} vCores";
                                }
                            }
                        }

                        // Fetch performance metrics
                        try
                        {
                            var endTime = DateTimeOffset.UtcNow;
                            var startTime = endTime.AddDays(-7);

                            // Determine which metrics to query based on tier and SKU name
                            var tier = db.Data.Sku?.Tier?.ToLower() ?? "";
                            var skuName = db.Data.Sku?.Name?.ToLower() ?? "";
                            string[] metricsToQuery;

                            if (tier.Contains("datawarehouse") || skuName.Contains("datawarehouse") || skuName.StartsWith("dw"))
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

                                if (metric.Name == "dtu_consumption_percent")
                                {
                                    resource.Metrics["DTU_Average_%"] = avgValue;
                                    resource.Metrics["DTU_Max_%"] = maxValue;
                                }
                                else if (metric.Name == "dwu_consumption_percent")
                                {
                                    resource.Metrics["DWU_Average_%"] = avgValue;
                                    resource.Metrics["DWU_Max_%"] = maxValue;
                                }
                                else if (metric.Name == "cpu_percent")
                                {
                                    resource.Metrics["CPU_Average_%"] = avgValue;
                                    resource.Metrics["CPU_Max_%"] = maxValue;
                                }
                                else if (metric.Name == "storage_percent")
                                {
                                    resource.Metrics["Storage_Average_%"] = avgValue;
                                    resource.Metrics["Storage_Max_%"] = maxValue;
                                }
                                else if (metric.Name == "memory_usage_percent")
                                {
                                    resource.Metrics["Memory_Average_%"] = avgValue;
                                    resource.Metrics["Memory_Max_%"] = maxValue;
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            SentrySdk.CaptureException(ex);
                            Console.WriteLine($"Could not fetch metrics for SQL DB {db.Data.Name}: {ex.Message}");
                        }

                        AddResource(resource);
                    }
                }
            }
            catch (Exception ex)
            {
                SentrySdk.CaptureException(ex);
                Console.WriteLine($"Error collecting SQL Databases: {ex.Message}");
            }
        }

        public async Task CollectVirtualMachines(SubscriptionResource subscription, ClientSecretCredential credential)
        {
            try
            {
                var metricsClient = new MetricsQueryClient(credential);
                var resourceGroups = subscription.GetResourceGroups();

                await foreach (var rg in resourceGroups)
                {
                    var vms = rg.GetGenericResourcesAsync(
                        filter: "resourceType eq 'Microsoft.Compute/virtualMachines'"
                    );

                    await foreach (var vm in vms)
                    {
                        var resource = new ResourceInfo
                        {
                            Type = "Virtual Machine",
                            Name = vm.Data.Name,
                            ResourceGroup = vm.Data.Id.ResourceGroupName ?? "Unknown",
                            Location = vm.Data.Location.ToString()
                        };

                        if (vm.Data.Properties != null)
                        {
                            var props = JsonSerializer.Deserialize<JsonElement>(vm.Data.Properties.ToString());

                            if (props.TryGetProperty("hardwareProfile", out var hwProfile))
                            {
                                if (hwProfile.TryGetProperty("vmSize", out var vmSize))
                                {
                                    resource.Properties["VMSize"] = vmSize.GetString() ?? "Unknown";
                                }
                            }

                            if (props.TryGetProperty("storageProfile", out var storageProfile))
                            {
                                if (storageProfile.TryGetProperty("osDisk", out var osDisk))
                                {
                                    if (osDisk.TryGetProperty("diskSizeGB", out var diskSize))
                                    {
                                        resource.Properties["OSDiskSizeGB"] = diskSize.GetInt32();
                                    }
                                }
                            }
                        }

                        // Fetch performance metrics
                        try
                        {
                            var endTime = DateTimeOffset.UtcNow;
                            var startTime = endTime.AddDays(-7);

                            var metricsResponse = await metricsClient.QueryResourceAsync(
                                vm.Id.ToString(),
                                new[] { "Percentage CPU", "Network In Total", "Network Out Total" },
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
                                    resource.Metrics["CPU_Average_%"] = avgValue;
                                    resource.Metrics["CPU_Max_%"] = maxValue;
                                }
                                else if (metric.Name == "Network In Total")
                                {
                                    resource.Metrics["Network_In_Average_MB"] = avgValue / 1024 / 1024;
                                    resource.Metrics["Network_In_Max_MB"] = maxValue / 1024 / 1024;
                                }
                                else if (metric.Name == "Network Out Total")
                                {
                                    resource.Metrics["Network_Out_Average_MB"] = avgValue / 1024 / 1024;
                                    resource.Metrics["Network_Out_Max_MB"] = maxValue / 1024 / 1024;
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            SentrySdk.CaptureException(ex);
                            Console.WriteLine($"Could not fetch metrics for VM {vm.Data.Name}: {ex.Message}");
                        }

                        AddResource(resource);
                    }
                }
            }
            catch (Exception ex)
            {
                SentrySdk.CaptureException(ex);
                Console.WriteLine($"Error collecting VMs: {ex.Message}");
            }
        }

        public async Task CollectStorageAccounts(SubscriptionResource subscription)
        {
            try
            {
                var resourceGroups = subscription.GetResourceGroups();

                await foreach (var rg in resourceGroups)
                {
                    var storageAccounts = rg.GetGenericResourcesAsync(
                        filter: "resourceType eq 'Microsoft.Storage/storageAccounts'"
                    );

                    await foreach (var storage in storageAccounts)
                    {
                        var resource = new ResourceInfo
                        {
                            Type = "Storage Account",
                            Name = storage.Data.Name,
                            ResourceGroup = storage.Data.Id.ResourceGroupName ?? "Unknown",
                            Location = storage.Data.Location.ToString()
                        };

                        if (storage.Data.Sku != null)
                        {
                            resource.Properties["SKU"] = storage.Data.Sku.Name;
                            resource.Properties["Tier"] = storage.Data.Sku.Tier ?? "Unknown";
                        }

                        if (storage.Data.Kind != null)
                        {
                            resource.Properties["Kind"] = storage.Data.Kind.ToString();
                        }

                        AddResource(resource);
                    }
                }
            }
            catch (Exception ex)
            {
                SentrySdk.CaptureException(ex);
                Console.WriteLine($"Error collecting Storage Accounts: {ex.Message}");
            }
        }

        public async Task CollectUnattachedDisks(SubscriptionResource subscription)
        {
            try
            {
                var resourceGroups = subscription.GetResourceGroups();

                await foreach (var rg in resourceGroups)
                {
                    var disks = rg.GetGenericResourcesAsync(
                        filter: "resourceType eq 'Microsoft.Compute/disks'"
                    );

                    await foreach (var disk in disks)
                    {
                        if (disk.Data.Properties != null)
                        {
                            var props = JsonSerializer.Deserialize<JsonElement>(disk.Data.Properties.ToString());

                            if (props.TryGetProperty("diskState", out var diskState))
                            {
                                var state = diskState.GetString();
                                if (state == "Unattached")
                                {
                                    var resource = new ResourceInfo
                                    {
                                        Type = "Unattached Disk",
                                        Name = disk.Data.Name,
                                        ResourceGroup = disk.Data.Id.ResourceGroupName ?? "Unknown",
                                        Location = disk.Data.Location.ToString()
                                    };

                                    if (disk.Data.Sku != null)
                                    {
                                        resource.Properties["SKU"] = disk.Data.Sku.Name;
                                    }

                                    if (props.TryGetProperty("diskSizeGB", out var diskSize))
                                    {
                                        resource.Properties["SizeGB"] = diskSize.GetInt32();
                                    }

                                    resource.Flags.Add("UNATTACHED - Consider deletion if not needed");

                                    AddResource(resource);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                SentrySdk.CaptureException(ex);
                Console.WriteLine($"Error collecting Unattached Disks: {ex.Message}");
            }
        }

        public async Task CollectUnusedPublicIPs(SubscriptionResource subscription)
        {
            try
            {
                var resourceGroups = subscription.GetResourceGroups();

                await foreach (var rg in resourceGroups)
                {
                    var publicIPs = rg.GetGenericResourcesAsync(
                        filter: "resourceType eq 'Microsoft.Network/publicIPAddresses'"
                    );

                    await foreach (var ip in publicIPs)
                    {
                        if (ip.Data.Properties != null)
                        {
                            var props = JsonSerializer.Deserialize<JsonElement>(ip.Data.Properties.ToString());

                            bool isAttached = props.TryGetProperty("ipConfiguration", out _);

                            if (!isAttached)
                            {
                                var resource = new ResourceInfo
                                {
                                    Type = "Unused Public IP",
                                    Name = ip.Data.Name,
                                    ResourceGroup = ip.Data.Id.ResourceGroupName ?? "Unknown",
                                    Location = ip.Data.Location.ToString()
                                };

                                if (ip.Data.Sku != null)
                                {
                                    resource.Properties["SKU"] = ip.Data.Sku.Name;
                                }

                                if (props.TryGetProperty("ipAddress", out var ipAddress))
                                {
                                    resource.Properties["IPAddress"] = ipAddress.GetString() ?? "Unknown";
                                }

                                if (props.TryGetProperty("publicIPAllocationMethod", out var allocMethod))
                                {
                                    resource.Properties["AllocationMethod"] = allocMethod.GetString() ?? "Unknown";
                                }

                                resource.Flags.Add("UNUSED - Not attached to any resource");

                                AddResource(resource);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                SentrySdk.CaptureException(ex);
                Console.WriteLine($"Error collecting Unused Public IPs: {ex.Message}");
            }
        }

        public async Task CollectKubernetesClusters(SubscriptionResource subscription)
        {
            try
            {
                var resourceGroups = subscription.GetResourceGroups();

                await foreach (var rg in resourceGroups)
                {
                    var aksClusters = rg.GetGenericResourcesAsync(
                        filter: "resourceType eq 'Microsoft.ContainerService/managedClusters'"
                    );

                    await foreach (var cluster in aksClusters)
                    {
                        var resource = new ResourceInfo
                        {
                            Type = "AKS Cluster",
                            Name = cluster.Data.Name,
                            ResourceGroup = cluster.Data.Id.ResourceGroupName ?? "Unknown",
                            Location = cluster.Data.Location.ToString()
                        };

                        if (cluster.Data.Properties != null)
                        {
                            var props = JsonSerializer.Deserialize<JsonElement>(cluster.Data.Properties.ToString());

                            if (props.TryGetProperty("kubernetesVersion", out var version))
                            {
                                resource.Properties["KubernetesVersion"] = version.GetString() ?? "Unknown";
                            }

                            if (props.TryGetProperty("agentPoolProfiles", out var agentPools))
                            {
                                var poolCount = 0;
                                var totalNodes = 0;
                                var poolDetails = new List<string>();

                                foreach (var pool in agentPools.EnumerateArray())
                                {
                                    poolCount++;
                                    var poolName = pool.TryGetProperty("name", out var pn) ? pn.GetString() : "Unknown";
                                    var count = pool.TryGetProperty("count", out var c) ? c.GetInt32() : 0;
                                    var vmSize = pool.TryGetProperty("vmSize", out var vm) ? vm.GetString() : "Unknown";

                                    totalNodes += count;
                                    poolDetails.Add($"{poolName}: {count} x {vmSize}");

                                    if (pool.TryGetProperty("enableAutoScaling", out var autoScale) && autoScale.GetBoolean())
                                    {
                                        var min = pool.TryGetProperty("minCount", out var minNode) ? minNode.GetInt32() : 0;
                                        var max = pool.TryGetProperty("maxCount", out var maxNode) ? maxNode.GetInt32() : 0;

                                        // Check if cluster is underutilized (running at min for extended period)
                                        if (count == min && max > min * 2)
                                        {
                                            resource.Flags.Add($"Pool '{poolName}' may be overprovisioned (max: {max}, current: {count})");
                                        }
                                    }
                                }

                                resource.Properties["NodePoolCount"] = poolCount;
                                resource.Properties["TotalNodes"] = totalNodes;
                                resource.Properties["NodePools"] = string.Join(", ", poolDetails);
                            }

                            if (props.TryGetProperty("networkProfile", out var network))
                            {
                                if (network.TryGetProperty("networkPlugin", out var plugin))
                                {
                                    resource.Properties["NetworkPlugin"] = plugin.GetString() ?? "Unknown";
                                }
                            }

                            if (props.TryGetProperty("addonProfiles", out var addons) && addons.ValueKind == JsonValueKind.Object)
                            {
                                var enabledAddons = new List<string>();
                                foreach (var addon in addons.EnumerateObject())
                                {
                                    if (addon.Value.TryGetProperty("enabled", out var enabled) && enabled.GetBoolean())
                                    {
                                        enabledAddons.Add(addon.Name);
                                    }
                                }
                                if (enabledAddons.Any())
                                {
                                    resource.Properties["EnabledAddons"] = string.Join(", ", enabledAddons);
                                }
                            }
                        }

                        AddResource(resource);
                    }
                }
            }
            catch (Exception ex)
            {
                SentrySdk.CaptureException(ex);
                Console.WriteLine($"Error collecting Kubernetes clusters: {ex.Message}");
            }
        }

        public async Task CollectRedisCaches(SubscriptionResource subscription, ClientSecretCredential credential)
        {
            try
            {
                var metricsClient = new MetricsQueryClient(credential);
                var resourceGroups = subscription.GetResourceGroups();

                await foreach (var rg in resourceGroups)
                {
                    var redisCaches = rg.GetGenericResourcesAsync(
                        filter: "resourceType eq 'Microsoft.Cache/Redis'"
                    );

                    await foreach (var cache in redisCaches)
                    {
                        var resource = new ResourceInfo
                        {
                            Type = "Redis Cache",
                            Name = cache.Data.Name,
                            ResourceGroup = cache.Data.Id.ResourceGroupName ?? "Unknown",
                            Location = cache.Data.Location.ToString()
                        };

                        if (cache.Data.Sku != null)
                        {
                            resource.Properties["Tier"] = cache.Data.Sku.Tier ?? "Unknown";
                            resource.Properties["SKU"] = cache.Data.Sku.Name ?? "Unknown";

                            if (cache.Data.Sku.Capacity.HasValue)
                            {
                                resource.Properties["Capacity"] = $"C{cache.Data.Sku.Capacity.Value}";
                            }
                        }

                        if (cache.Data.Properties != null)
                        {
                            var props = JsonSerializer.Deserialize<JsonElement>(cache.Data.Properties.ToString());

                            if (props.TryGetProperty("redisVersion", out var version))
                            {
                                resource.Properties["RedisVersion"] = version.GetString() ?? "Unknown";
                            }

                            if (props.TryGetProperty("enableNonSslPort", out var nonSsl) && nonSsl.GetBoolean())
                            {
                                resource.Properties["NonSslPortEnabled"] = "true";
                                resource.Flags.Add("Non-SSL port enabled - Security risk, consider disabling");
                            }

                            if (props.TryGetProperty("provisioningState", out var provisioningState))
                            {
                                resource.Properties["ProvisioningState"] = provisioningState.GetString() ?? "Unknown";
                            }

                            if (props.TryGetProperty("sku", out var skuProps))
                            {
                                if (skuProps.TryGetProperty("family", out var family))
                                {
                                    resource.Properties["Family"] = family.GetString() ?? "Unknown";
                                }
                            }
                        }

                        // Fetch performance metrics
                        try
                        {
                            var endTime = DateTimeOffset.UtcNow;
                            var startTime = endTime.AddDays(-7);

                            var metricsResponse = await metricsClient.QueryResourceAsync(
                                cache.Id.ToString(),
                                new[] { "percentProcessorTime", "usedmemorypercentage", "cacheLatency", "connectedclients", "totalcommandsprocessed", "cachehits", "cachemisses", "evictedkeys", "expiredkeys", "serverLoad" },
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

                                var totalValue = timeSeries
                                    .Where(v => v.Total.HasValue)
                                    .Select(v => v.Total.Value)
                                    .DefaultIfEmpty(0)
                                    .Sum();

                                if (metric.Name == "percentProcessorTime")
                                {
                                    resource.Metrics["CPU_Average_%"] = avgValue;
                                    resource.Metrics["CPU_Max_%"] = maxValue;

                                    if (maxValue > 90)
                                    {
                                        resource.Flags.Add($"High CPU usage detected: {maxValue:F2}% max");
                                    }
                                    else if (avgValue < 10)
                                    {
                                        resource.Flags.Add($"Low CPU usage: {avgValue:F2}% average - Consider downsizing");
                                    }
                                }
                                else if (metric.Name == "usedmemorypercentage")
                                {
                                    resource.Metrics["Memory_Average_%"] = avgValue;
                                    resource.Metrics["Memory_Max_%"] = maxValue;

                                    if (maxValue > 90)
                                    {
                                        resource.Flags.Add($"High memory usage detected: {maxValue:F2}% max");
                                    }
                                }
                                else if (metric.Name == "cacheLatency")
                                {
                                    resource.Metrics["Latency_Average_Microseconds"] = avgValue;
                                    resource.Metrics["Latency_Max_Microseconds"] = maxValue;
                                }
                                else if (metric.Name == "connectedclients")
                                {
                                    resource.Metrics["Connected_Clients_Average"] = avgValue;
                                    resource.Metrics["Connected_Clients_Max"] = maxValue;
                                }
                                else if (metric.Name == "totalcommandsprocessed")
                                {
                                    resource.Metrics["Commands_Total"] = totalValue;

                                    if (totalValue < 1000)
                                    {
                                        resource.Flags.Add($"Low usage: {totalValue:F0} total commands in 7 days - Consider if needed");
                                    }
                                }
                                else if (metric.Name == "cachehits")
                                {
                                    resource.Metrics["Cache_Hits_Total"] = totalValue;
                                }
                                else if (metric.Name == "cachemisses")
                                {
                                    resource.Metrics["Cache_Misses_Total"] = totalValue;

                                    // Calculate hit ratio if we have both hits and misses
                                    if (resource.Metrics.ContainsKey("Cache_Hits_Total"))
                                    {
                                        var hits = resource.Metrics["Cache_Hits_Total"];
                                        var totalRequests = hits + totalValue;

                                        if (totalRequests > 0)
                                        {
                                            var hitRatio = (hits / totalRequests) * 100;
                                            resource.Metrics["Cache_Hit_Ratio_%"] = hitRatio;

                                            if (hitRatio < 70)
                                            {
                                                resource.Flags.Add($"Low cache hit ratio: {hitRatio:F2}% - Review caching strategy");
                                            }
                                        }
                                    }
                                }
                                else if (metric.Name == "evictedkeys")
                                {
                                    resource.Metrics["Evicted_Keys_Total"] = totalValue;

                                    if (totalValue > 1000)
                                    {
                                        resource.Flags.Add($"High key eviction: {totalValue:F0} keys evicted - Consider increasing memory");
                                    }
                                }
                                else if (metric.Name == "expiredkeys")
                                {
                                    resource.Metrics["Expired_Keys_Total"] = totalValue;
                                }
                                else if (metric.Name == "serverLoad")
                                {
                                    resource.Metrics["Server_Load_Average_%"] = avgValue;
                                    resource.Metrics["Server_Load_Max_%"] = maxValue;

                                    if (maxValue > 80)
                                    {
                                        resource.Flags.Add($"High server load: {maxValue:F2}% max");
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            SentrySdk.CaptureException(ex);
                            Console.WriteLine($"Could not fetch metrics for Redis Cache {cache.Data.Name}: {ex.Message}");
                        }

                        AddResource(resource);
                    }
                }
            }
            catch (Exception ex)
            {
                SentrySdk.CaptureException(ex);
                Console.WriteLine($"Error collecting Redis Caches: {ex.Message}");
            }
        }
    }
}
