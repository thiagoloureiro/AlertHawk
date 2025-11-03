using System.Text.Json;
using AlertHawk.Metrics;
using k8s;

// Read collection interval from environment variable (default: 30 seconds)
var collectionIntervalSeconds = int.TryParse(
    Environment.GetEnvironmentVariable("METRICS_COLLECTION_INTERVAL_SECONDS"),
    out var interval) && interval > 0
    ? interval
    : 30;

Console.WriteLine($"Starting metrics collection service (interval: {collectionIntervalSeconds} seconds)");
Console.WriteLine("Press Ctrl+C to stop...");
Console.WriteLine();

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

try
{
    while (!cancellationTokenSource.Token.IsCancellationRequested)
    {
        await CollectMetricsAsync(client, namespacesToWatch);
        
        // Wait for the specified interval before next collection
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

static async Task CollectMetricsAsync(Kubernetes client, string[] namespacesToWatch)
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