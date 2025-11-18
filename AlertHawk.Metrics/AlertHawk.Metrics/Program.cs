using System.Text.Json;
using AlertHawk.Metrics;
using k8s;

// Read configuration from environment variables
var collectionIntervalSeconds = int.TryParse(
    Environment.GetEnvironmentVariable("METRICS_COLLECTION_INTERVAL_SECONDS"),
    out var interval) && interval > 0
    ? interval
    : 30;

var clickHouseConnectionString = Environment.GetEnvironmentVariable("CLICKHOUSE_CONNECTION_STRING")
    ?? "Host=localhost;Port=8123;Database=default;Username=default;Password=";

var clickHouseTableName = Environment.GetEnvironmentVariable("CLICKHOUSE_TABLE_NAME")
    ?? "k8s_metrics";

var clusterName = Environment.GetEnvironmentVariable("CLUSTER_NAME");
if (string.IsNullOrWhiteSpace(clusterName))
{
    Console.Error.WriteLine("ERROR: CLUSTER_NAME environment variable is required but not set!");
    Console.Error.WriteLine("Please set the CLUSTER_NAME environment variable before starting the application.");
    Environment.Exit(1);
}

Console.WriteLine($"Starting metrics collection service (interval: {collectionIntervalSeconds} seconds)");
Console.WriteLine($"Cluster name: {clusterName}");
Console.WriteLine($"ClickHouse connection: {clickHouseConnectionString.Replace("Password=", "Password=***")}");
Console.WriteLine($"ClickHouse table: {clickHouseTableName}");
Console.WriteLine("Press Ctrl+C to stop...");
Console.WriteLine();

// Initialize ClickHouse service
using var clickHouseService = new ClickHouseService(clickHouseConnectionString, clusterName, clickHouseTableName);

var config = KubernetesClientConfiguration.InClusterConfig();
var client = new Kubernetes(config);
var namespacesToWatch = new[] { "alerthawk", "traefik", "ilstudio" };

var cancellationTokenSource = new CancellationTokenSource();
Console.CancelKeyPress += (sender, e) =>
{
    e.Cancel = true;
    Console.WriteLine("\nShutting down gracefully...");
    cancellationTokenSource.Cancel();
};

// Start metrics collection loop
try
{
    while (!cancellationTokenSource.Token.IsCancellationRequested)
    {
        await CollectMetricsAsync(client, namespacesToWatch, clickHouseService);
        await CollectNodeMetricsAsync(client, clickHouseService);
        await Task.Delay(TimeSpan.FromSeconds(collectionIntervalSeconds), cancellationTokenSource.Token);
    }
}
catch (OperationCanceledException)
{
    Console.WriteLine("Metrics collection stopped.");
}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex.Message}");
    Environment.Exit(1);
}
finally
{
    cancellationTokenSource.Cancel();
}

static async Task CollectMetricsAsync(
    Kubernetes client, 
    string[] namespacesToWatch,
    ClickHouseService clickHouseService)
{
    var jsonOptions = new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true
    };

    try
    {
        Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Collecting metrics...");

        foreach (var ns in namespacesToWatch)
        {
            try
            {
                var pods = await client.CoreV1.ListNamespacedPodAsync(ns);

                // Build a dictionary of pod name -> container CPU limits
                var podCpuLimits = new Dictionary<string, Dictionary<string, string>>();
                foreach (var pod in pods.Items)
                {
                    var containerLimits = new Dictionary<string, string>();
                    if (pod.Spec?.Containers != null)
                    {
                        foreach (var container in pod.Spec.Containers)
                        {
                            // Prefer limit over request, and convert to string format
                            var cpuLimit = container.Resources?.Limits?.ContainsKey("cpu") == true
                                ? container.Resources.Limits["cpu"].ToString()
                                : container.Resources?.Requests?.ContainsKey("cpu") == true
                                    ? container.Resources.Requests["cpu"].ToString()
                                    : null;
                            
                            if (cpuLimit != null)
                            {
                                containerLimits[container.Name] = cpuLimit;
                            }
                        }
                    }
                    podCpuLimits[pod.Metadata.Name] = containerLimits;
                }

                foreach (var pod in pods.Items)
                {
                    Console.WriteLine($"{pod.Metadata.NamespaceProperty}/{pod.Metadata.Name} - {pod.Status.Phase}");
                }

                var response = await client.CustomObjects.ListClusterCustomObjectAsync(
                    group: "metrics.k8s.io",
                    version: "v1beta1",
                    plural: "pods");

                var jsonString = JsonSerializer.Serialize(response);
                var podMetricsList = JsonSerializer.Deserialize<PodMetricsList>(jsonString, jsonOptions);

                if (podMetricsList != null)
                {
                    Console.WriteLine($"Found {podMetricsList.Items.Length} pod metrics");
                    foreach (var item in podMetricsList.Items)
                    {
                        // Only show metrics for pods in current namespace
                        if (item.Metadata.Namespace != ns)
                            continue;

                        Console.WriteLine($"Pod: {item.Metadata.Namespace}/{item.Metadata.Name} - Timestamp: {item.Timestamp:yyyy-MM-dd HH:mm:ss}");
                        foreach (var container in item.Containers)
                        {
                            // Get CPU limit for this container
                            var cpuLimit = podCpuLimits.TryGetValue(item.Metadata.Name, out var containerLimits) &&
                                         containerLimits.TryGetValue(container.Name, out var limit)
                                ? limit
                                : null;

                            // Write metrics to ClickHouse
                            var cpuCores = ResourceFormatter.ParseCpuToCores(container.Usage.Cpu);
                            var memoryBytes = ParseMemoryToBytes(container.Usage.Memory);
                            double? cpuLimitCores = null;

                            if (cpuLimit != null)
                            {
                                cpuLimitCores = ResourceFormatter.ParseCpuToCores(cpuLimit);
                            }

                            if (memoryBytes > 0)
                            {
                                await clickHouseService.WriteMetricsAsync(
                                    item.Metadata.Namespace,
                                    item.Metadata.Name,
                                    container.Name,
                                    cpuCores,
                                    cpuLimitCores,
                                    memoryBytes);
                            }

                            var formattedCpu = ResourceFormatter.FormatCpu(container.Usage.Cpu, cpuLimit);
                            var formattedMemory = ResourceFormatter.FormatMemory(container.Usage.Memory);
                            Console.WriteLine($"  Container: {container.Name} - CPU: {formattedCpu}, Memory: {formattedMemory}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error collecting metrics for namespace '{ns}': {ex.Message}");
            }
        }

        Console.WriteLine();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error during metrics collection: {ex.Message}");
    }
}

static async Task CollectNodeMetricsAsync(
    Kubernetes client,
    ClickHouseService clickHouseService)
{
    var jsonOptions = new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true
    };

    try
    {
        Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Collecting node metrics...");

        // Get node list to retrieve capacity information
        var nodes = await client.CoreV1.ListNodeAsync();
        var nodeCapacities = new Dictionary<string, (double cpuCores, double memoryBytes)>();

        foreach (var node in nodes.Items)
        {
            var cpuCapacity = 0.0;
            var memoryCapacity = 0.0;

            if (node.Status?.Capacity != null)
            {
                // Parse CPU capacity
                if (node.Status.Capacity.ContainsKey("cpu"))
                {
                    var cpuStr = node.Status.Capacity["cpu"].ToString();
                    cpuCapacity = ResourceFormatter.ParseCpuToCores(cpuStr ?? "0");
                }

                // Parse memory capacity
                if (node.Status.Capacity.ContainsKey("memory"))
                {
                    var memoryStr = node.Status.Capacity["memory"].ToString();
                    memoryCapacity = ParseMemoryToBytes(memoryStr);
                }
            }

            if (node.Metadata?.Name != null)
            {
                nodeCapacities[node.Metadata.Name] = (cpuCapacity, memoryCapacity);
            }
        }

        // Get node metrics from metrics API
        var nodeMetricsResponse = await client.CustomObjects.ListClusterCustomObjectAsync(
            group: "metrics.k8s.io",
            version: "v1beta1",
            plural: "nodes");

        var nodeMetricsJson = JsonSerializer.Serialize(nodeMetricsResponse);
        var nodeMetricsList = JsonSerializer.Deserialize<NodeMetricsList>(nodeMetricsJson, jsonOptions);

        if (nodeMetricsList != null)
        {
            Console.WriteLine($"Found {nodeMetricsList.Items.Length} node metrics");
            foreach (var nodeMetric in nodeMetricsList.Items)
            {
                var nodeName = nodeMetric.Metadata.Name;
                var cpuUsageCores = ResourceFormatter.ParseCpuToCores(nodeMetric.Usage.Cpu);
                var memoryUsageBytes = ParseMemoryToBytes(nodeMetric.Usage.Memory);

                // Get capacity for this node
                var (cpuCapacityCores, memoryCapacityBytes) = nodeCapacities.TryGetValue(nodeName, out var capacity)
                    ? capacity
                    : (0.0, 0.0);

                if (memoryUsageBytes > 0)
                {
                    await clickHouseService.WriteNodeMetricsAsync(
                        nodeName,
                        cpuUsageCores,
                        cpuCapacityCores,
                        memoryUsageBytes,
                        memoryCapacityBytes);

                    var cpuPercent = cpuCapacityCores > 0 ? (cpuUsageCores / cpuCapacityCores * 100) : 0;
                    var memoryPercent = memoryCapacityBytes > 0 ? (memoryUsageBytes / memoryCapacityBytes * 100) : 0;

                    Console.WriteLine($"  Node: {nodeName} - CPU: {cpuUsageCores:F2}/{cpuCapacityCores:F2} cores ({cpuPercent:F1}%), Memory: {ResourceFormatter.FormatMemory(nodeMetric.Usage.Memory)} ({memoryPercent:F1}%)");
                }
            }
        }

        Console.WriteLine();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error during node metrics collection: {ex.Message}");
    }
}

static double ParseMemoryToBytes(string? memoryValue)
{
    if (string.IsNullOrWhiteSpace(memoryValue))
        return 0;

    memoryValue = memoryValue.Trim();
    double value = 0;

    if (memoryValue.EndsWith("Ki", StringComparison.OrdinalIgnoreCase))
    {
        var numericPart = memoryValue.Substring(0, memoryValue.Length - 2);
        if (double.TryParse(numericPart, out value))
            return value * 1024;
    }
    else if (memoryValue.EndsWith("Mi", StringComparison.OrdinalIgnoreCase))
    {
        var numericPart = memoryValue.Substring(0, memoryValue.Length - 2);
        if (double.TryParse(numericPart, out value))
            return value * 1024 * 1024;
    }
    else if (memoryValue.EndsWith("Gi", StringComparison.OrdinalIgnoreCase))
    {
        var numericPart = memoryValue.Substring(0, memoryValue.Length - 2);
        if (double.TryParse(numericPart, out value))
            return value * 1024 * 1024 * 1024;
    }
    else if (memoryValue.EndsWith("Ti", StringComparison.OrdinalIgnoreCase))
    {
        var numericPart = memoryValue.Substring(0, memoryValue.Length - 2);
        if (double.TryParse(numericPart, out value))
            return value * 1024L * 1024 * 1024 * 1024;
    }
    else if (double.TryParse(memoryValue, out value))
    {
        return value; // Assume bytes if no unit
    }

    return 0;
}