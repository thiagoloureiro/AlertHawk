using System.Text.Json;
using AlertHawk.Metrics;
using k8s;
using Prometheus;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

// Read configuration from environment variables
var collectionIntervalSeconds = int.TryParse(
    Environment.GetEnvironmentVariable("METRICS_COLLECTION_INTERVAL_SECONDS"),
    out var interval) && interval > 0
    ? interval
    : 30;

var metricsPort = int.TryParse(
    Environment.GetEnvironmentVariable("METRICS_PORT"),
    out var port) && port > 0
    ? port
    : 8080;

Console.WriteLine($"Starting metrics collection service (interval: {collectionIntervalSeconds} seconds)");
Console.WriteLine($"Prometheus metrics endpoint: http://0.0.0.0:{metricsPort}/metrics");
Console.WriteLine("Press Ctrl+C to stop...");
Console.WriteLine();

// Create Prometheus metrics
var cpuUsageGauge = Metrics.CreateGauge(
    "k8s_container_cpu_usage_cores",
    "CPU usage in cores",
    new[] { "namespace", "pod", "container" });

var cpuLimitGauge = Metrics.CreateGauge(
    "k8s_container_cpu_limit_cores",
    "CPU limit in cores",
    new[] { "namespace", "pod", "container" });

var memoryUsageGauge = Metrics.CreateGauge(
    "k8s_container_memory_usage_bytes",
    "Memory usage in bytes",
    new[] { "namespace", "pod", "container" });

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

// Start Prometheus metrics server
var builder = WebApplication.CreateBuilder();
builder.Services.AddRouting();
var app = builder.Build();
app.UseRouting();
app.UseHttpMetrics();
app.MapMetrics();
app.MapGet("/", () => "AlertHawk Metrics Collector - Prometheus endpoint: /metrics");

var metricsServerTask = app.RunAsync($"http://0.0.0.0:{metricsPort}");

// Start metrics collection loop
try
{
    var collectionTask = Task.Run(async () =>
    {
        while (!cancellationTokenSource.Token.IsCancellationRequested)
        {
            await CollectMetricsAsync(client, namespacesToWatch, cpuUsageGauge, cpuLimitGauge, memoryUsageGauge);
            await Task.Delay(TimeSpan.FromSeconds(collectionIntervalSeconds), cancellationTokenSource.Token);
        }
    }, cancellationTokenSource.Token);

    await Task.WhenAny(metricsServerTask, collectionTask);
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
    Gauge cpuUsageGauge,
    Gauge cpuLimitGauge,
    Gauge memoryUsageGauge)
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

                            // Record Prometheus metrics
                            var cpuCores = ResourceFormatter.ParseCpuToCores(container.Usage.Cpu);
                            cpuUsageGauge.WithLabels(item.Metadata.Namespace, item.Metadata.Name, container.Name).Set(cpuCores);

                            if (cpuLimit != null)
                            {
                                var limitCores = ResourceFormatter.ParseCpuToCores(cpuLimit);
                                cpuLimitGauge.WithLabels(item.Metadata.Namespace, item.Metadata.Name, container.Name).Set(limitCores);
                            }

                            var memoryBytes = ParseMemoryToBytes(container.Usage.Memory);
                            if (memoryBytes > 0)
                            {
                                memoryUsageGauge.WithLabels(item.Metadata.Namespace, item.Metadata.Name, container.Name).Set(memoryBytes);
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