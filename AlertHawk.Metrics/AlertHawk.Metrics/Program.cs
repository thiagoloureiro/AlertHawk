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
                        Console.WriteLine($"Pod: {item.Metadata.Namespace}/{item.Metadata.Name} - Timestamp: {item.Timestamp:yyyy-MM-dd HH:mm:ss}");
                        foreach (var container in item.Containers)
                        {
                            var formattedCpu = ResourceFormatter.FormatCpu(container.Usage.Cpu);
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