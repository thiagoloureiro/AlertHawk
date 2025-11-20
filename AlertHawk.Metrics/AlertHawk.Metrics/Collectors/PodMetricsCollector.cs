using System.Text.Json;
using AlertHawk.Metrics;
using k8s;
using Serilog;

namespace AlertHawk.Metrics.Collectors;

public static class PodMetricsCollector
{
    public static async Task CollectAsync(
        Kubernetes client,
        string[] namespacesToWatch,
        MetricsApiClient apiClient)
    {
        await CollectAsync(new KubernetesClientWrapper(client), namespacesToWatch, apiClient);
    }

    public static async Task CollectAsync(
        IKubernetesClientWrapper clientWrapper,
        string[] namespacesToWatch,
        IMetricsApiClient apiClient)
    {
        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        try
        {
            Log.Information("Collecting pod metrics...");

            foreach (var ns in namespacesToWatch)
            {
                try
                {
                    var pods = await clientWrapper.ListNamespacedPodAsync(ns);

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
                        Log.Debug("Pod: {Namespace}/{PodName} - Phase: {Phase}", 
                            pod.Metadata.NamespaceProperty, pod.Metadata.Name, pod.Status.Phase);
                    }

                    var response = await clientWrapper.ListClusterCustomObjectAsync(
                        group: "metrics.k8s.io",
                        version: "v1beta1",
                        plural: "pods");

                    var jsonString = JsonSerializer.Serialize(response);
                    var podMetricsList = JsonSerializer.Deserialize<PodMetricsList>(jsonString, jsonOptions);

                    if (podMetricsList != null)
                    {
                        Log.Debug("Found {Count} pod metrics", podMetricsList.Items.Length);
                        foreach (var item in podMetricsList.Items)
                        {
                            // Only show metrics for pods in current namespace
                            if (item.Metadata.Namespace != ns)
                                continue;

                            Log.Debug("Pod: {Namespace}/{PodName} - Timestamp: {Timestamp}", 
                                item.Metadata.Namespace, item.Metadata.Name, item.Timestamp);
                            foreach (var container in item.Containers)
                            {
                                // Get CPU limit for this container
                                var cpuLimit = podCpuLimits.TryGetValue(item.Metadata.Name, out var containerLimits) &&
                                             containerLimits.TryGetValue(container.Name, out var limit)
                                    ? limit
                                    : null;

                                // Write metrics to ClickHouse
                                var cpuCores = ResourceFormatter.ParseCpuToCores(container.Usage.Cpu);
                                var memoryBytes = Utils.MemoryParser.ParseToBytes(container.Usage.Memory);
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
                                        Log.Error(ex, "Error sending pod metric to API for {Namespace}/{PodName}/{Container}", 
                                            item.Metadata.Namespace, item.Metadata.Name, container.Name);
                                    }
                                }

                                var formattedCpu = ResourceFormatter.FormatCpu(container.Usage.Cpu, cpuLimit);
                                var formattedMemory = ResourceFormatter.FormatMemory(container.Usage.Memory);
                                Log.Debug("Container: {Container} - CPU: {Cpu}, Memory: {Memory}", 
                                    container.Name, formattedCpu, formattedMemory);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error collecting metrics for namespace '{Namespace}'", ns);
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error during pod metrics collection");
        }
    }
}

