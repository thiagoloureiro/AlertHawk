using System.Net.Http.Headers;
using System.Text.Json;
using AlertHawk.Metrics;
using k8s;

SentrySdk.Init(options =>
{
    // A Sentry Data Source Name (DSN) is required.
    // See https://docs.sentry.io/product/sentry-basics/dsn-explainer/
    // You can set it in the SENTRY_DSN environment variable, or you can set it in code here.
    options.Dsn = "https://7539147312d4c51ccf970c6ddd0f15ca@o418696.ingest.us.sentry.io/4510386963283968";

    // When debug is enabled, the Sentry client will emit detailed debugging information to the console.
    // This might be helpful, or might interfere with the normal operation of your application.
    // We enable it here for demonstration purposes when first trying Sentry.
    // You shouldn't do this in your applications unless you're troubleshooting issues with Sentry.
    options.Debug = false;

    // This option is recommended. It enables Sentry's "Release Health" feature.
    options.AutoSessionTracking = true;
});

// Read configuration from environment variables
var collectionIntervalSeconds = int.TryParse(
    Environment.GetEnvironmentVariable("METRICS_COLLECTION_INTERVAL_SECONDS"),
    out var interval) && interval > 0
    ? interval
    : 30;

var apiBaseUrl = Environment.GetEnvironmentVariable("METRICS_API_URL")
    ?? "http://localhost:5000";

var clusterName = Environment.GetEnvironmentVariable("CLUSTER_NAME");
if (string.IsNullOrWhiteSpace(clusterName))
{
    Console.Error.WriteLine("ERROR: CLUSTER_NAME environment variable is required but not set!");
    Console.Error.WriteLine("Please set the CLUSTER_NAME environment variable before starting the application.");
    Environment.Exit(1);
}

Console.WriteLine($"Starting metrics collection service (interval: {collectionIntervalSeconds} seconds)");
Console.WriteLine($"Cluster name: {clusterName}");
Console.WriteLine($"Metrics API URL: {apiBaseUrl}");
Console.WriteLine("Press Ctrl+C to stop...");
Console.WriteLine();

// Initialize API client
using var apiClient = new MetricsApiClient(apiBaseUrl, clusterName);

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
        await CollectMetricsAsync(client, namespacesToWatch, apiClient);
        await CollectNodeMetricsAsync(client, apiClient);
        await CollectPvcMetricsAsync(client, namespacesToWatch, apiClient);
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
    MetricsApiClient apiClient)
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
                                try
                                {
                                    await apiClient.WritePodMetricAsync(
                                        item.Metadata.Namespace,
                                        item.Metadata.Name,
                                        container.Name,
                                        cpuCores,
                                        cpuLimitCores,
                                        memoryBytes);
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine($"Error sending pod metric to API: {ex.Message}");
                                }
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
    MetricsApiClient apiClient)
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
                    try
                    {
                        await apiClient.WriteNodeMetricAsync(
                            nodeName,
                            cpuUsageCores,
                            cpuCapacityCores,
                            memoryUsageBytes,
                            memoryCapacityBytes);

                        var cpuPercent = cpuCapacityCores > 0 ? (cpuUsageCores / cpuCapacityCores * 100) : 0;
                        var memoryPercent = memoryCapacityBytes > 0 ? (memoryUsageBytes / memoryCapacityBytes * 100) : 0;

                        Console.WriteLine($"  Node: {nodeName} - CPU: {cpuUsageCores:F2}/{cpuCapacityCores:F2} cores ({cpuPercent:F1}%), Memory: {ResourceFormatter.FormatMemory(nodeMetric.Usage.Memory)} ({memoryPercent:F1}%)");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error sending node metric to API: {ex.Message}");
                    }
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

static async Task CollectPvcMetricsAsync(
    Kubernetes client,
    string[] namespacesToWatch,
    MetricsApiClient apiClient)
{
    try
    {
        Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Collecting PVC metrics...");

        // Cache Kubelet metrics per node to avoid repeated calls
        var kubeletMetricsCache = new Dictionary<string, string?>();

        foreach (var ns in namespacesToWatch)
        {
            try
            {
                var pvcs = await client.CoreV1.ListNamespacedPersistentVolumeClaimAsync(ns);

                Console.WriteLine($"Found {pvcs.Items.Count} PVCs in namespace '{ns}'");

                foreach (var pvc in pvcs.Items)
                {
                    var pvcName = pvc.Metadata?.Name ?? "unknown";
                    var status = pvc.Status?.Phase ?? "Unknown";
                    var storageClass = pvc.Spec?.StorageClassName ?? "unknown";
                    var volumeName = pvc.Spec?.VolumeName;

                    // Parse capacity
                    double capacityBytes = 0;
                    if (pvc.Status?.Capacity != null && pvc.Status.Capacity.ContainsKey("storage"))
                    {
                        var capacityStr = pvc.Status.Capacity["storage"].ToString();
                        capacityBytes = ParseMemoryToBytes(capacityStr);
                    }

                    // Try to fetch usedBytes from Kubelet metrics endpoint
                    double? usedBytes = await GetPvcUsedBytesFromKubeletAsync(client, ns, pvcName, kubeletMetricsCache);

                    if (capacityBytes > 0)
                    {
                        try
                        {
                            await apiClient.WritePvcMetricAsync(
                                ns,
                                pvcName,
                                storageClass,
                                status,
                                capacityBytes,
                                usedBytes,
                                volumeName);

                            var capacityFormatted = ResourceFormatter.FormatMemory(pvc.Status?.Capacity?.ContainsKey("storage") == true 
                                ? pvc.Status.Capacity["storage"].ToString() 
                                : capacityBytes.ToString());
                            var usedFormatted = usedBytes.HasValue ? ResourceFormatter.FormatMemory(usedBytes.Value.ToString()) : "N/A";
                            Console.WriteLine($"  PVC: {ns}/{pvcName} - Status: {status}, StorageClass: {storageClass}, Capacity: {capacityFormatted}, Used: {usedFormatted}");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error sending PVC metric to API: {ex.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error collecting PVC metrics for namespace '{ns}': {ex.Message}");
            }
        }

        Console.WriteLine();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error during PVC metrics collection: {ex.Message}");
    }
}

static async Task<double?> GetPvcUsedBytesFromKubeletAsync(
    Kubernetes client,
    string @namespace,
    string pvcName,
    Dictionary<string, string?> metricsCache)
{
    try
    {
        // Get list of nodes
        var nodes = await client.CoreV1.ListNodeAsync();
        
        if (nodes.Items.Count == 0)
        {
            return null;
        }

        // Use the Kubernetes client's HttpClient directly for authenticated requests
        // The Kubernetes client handles authentication automatically
        var httpClient = client.HttpClient;

        // Try to get metrics from each node until we find the metric
        foreach (var node in nodes.Items)
        {
            var nodeName = node.Metadata?.Name;
            if (string.IsNullOrWhiteSpace(nodeName))
                continue;

            try
            {
                // Check cache first
                if (!metricsCache.TryGetValue(nodeName, out var metricsText))
                {
                    // Fetch metrics from Kubelet via Kubernetes API proxy
                    // Endpoint: /api/v1/nodes/{nodeName}/proxy/metrics
                    try
                    {
                        // Use the Kubernetes API proxy endpoint
                        var baseUri = client.BaseUri?.ToString().TrimEnd('/') ?? "https://kubernetes.default.svc";
                        var proxyUrl = $"{baseUri}/api/v1/nodes/{nodeName}/proxy/metrics";
                        var response = await httpClient.GetAsync(proxyUrl);
                        
                        if (response.IsSuccessStatusCode)
                        {
                            metricsText = await response.Content.ReadAsStringAsync();
                            metricsCache[nodeName] = metricsText;
                        }
                        else
                        {
                            Console.WriteLine($"  Warning: Failed to fetch metrics from node '{nodeName}': {response.StatusCode}");
                            metricsCache[nodeName] = null;
                            continue;
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"  Warning: Could not fetch metrics via proxy for node '{nodeName}': {ex.Message}");
                        metricsCache[nodeName] = null;
                        continue;
                    }
                }

                if (string.IsNullOrWhiteSpace(metricsText))
                    continue;

                // Parse the metric value
                var usedBytes = ParseKubeletVolumeStatsUsedBytes(metricsText, @namespace, pvcName);
                if (usedBytes.HasValue)
                {
                    return usedBytes;
                }
            }
            catch (Exception ex)
            {
                // Log but continue to next node
                Console.WriteLine($"  Warning: Could not fetch Kubelet metrics from node '{nodeName}': {ex.Message}");
            }
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"  Warning: Error fetching Kubelet metrics: {ex.Message}");
    }

    return null;
}

static double? ParseKubeletVolumeStatsUsedBytes(string metricsText, string @namespace, string pvcName)
{
    if (string.IsNullOrWhiteSpace(metricsText))
        return null;

    // Look for the metric: kubelet_volume_stats_used_bytes{namespace="<namespace>",persistentvolumeclaim="<pvc-name>"} <value>
    var metricName = "kubelet_volume_stats_used_bytes";
    var lines = metricsText.Split('\n', StringSplitOptions.RemoveEmptyEntries);

    foreach (var line in lines)
    {
        var trimmedLine = line.Trim();
        
        // Skip comments
        if (trimmedLine.StartsWith('#'))
            continue;

        // Check if this line contains our metric
        if (!trimmedLine.StartsWith(metricName))
            continue;

        // Parse the metric line
        // Format: kubelet_volume_stats_used_bytes{namespace="<ns>",persistentvolumeclaim="<pvc>"} <value>
        var openBraceIndex = trimmedLine.IndexOf('{');
        if (openBraceIndex < 0)
            continue;

        var closeBraceIndex = trimmedLine.IndexOf('}', openBraceIndex);
        if (closeBraceIndex < 0)
            continue;

        // Extract labels
        var labelsSection = trimmedLine.Substring(openBraceIndex + 1, closeBraceIndex - openBraceIndex - 1);
        
        // Check if namespace and pvcName match
        var namespaceMatch = labelsSection.Contains($"namespace=\"{@namespace}\"", StringComparison.OrdinalIgnoreCase) ||
                            labelsSection.Contains($"namespace='{@namespace}'", StringComparison.OrdinalIgnoreCase);
        var pvcMatch = labelsSection.Contains($"persistentvolumeclaim=\"{pvcName}\"", StringComparison.OrdinalIgnoreCase) ||
                      labelsSection.Contains($"persistentvolumeclaim='{pvcName}'", StringComparison.OrdinalIgnoreCase);

        if (!namespaceMatch || !pvcMatch)
            continue;

        // Extract the value (after the closing brace)
        var valueStart = closeBraceIndex + 1;
        if (valueStart >= trimmedLine.Length)
            continue;

        var valuePart = trimmedLine.Substring(valueStart).Trim();
        
        // The value might be followed by a timestamp in some formats, so take the first part
        var valueStr = valuePart.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
        
        if (string.IsNullOrWhiteSpace(valueStr))
            continue;

        // Try to parse the value
        if (double.TryParse(valueStr, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var value))
        {
            return value;
        }
    }

    return null;
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