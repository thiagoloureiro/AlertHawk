using System.Diagnostics;
using System.Text.Json;
using AlertHawk.Metrics;
using k8s;
using k8s.Models;
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

        var collectLogs = Environment.GetEnvironmentVariable("COLLECT_LOGS");
        var isLogCollectionEnabled = !string.IsNullOrWhiteSpace(collectLogs) &&
                                      collectLogs.Equals("true", StringComparison.OrdinalIgnoreCase);

        var sw = Stopwatch.StartNew();
        try
        {
            Log.Information("Collecting pod metrics...");

            PodMetricsList? podMetricsList = null;
            try
            {
                var response = await clientWrapper.ListClusterCustomObjectAsync(
                    group: "metrics.k8s.io",
                    version: "v1beta1",
                    plural: "pods");

                var jsonString = JsonSerializer.Serialize(response);
                podMetricsList = JsonSerializer.Deserialize<PodMetricsList>(jsonString, jsonOptions);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error fetching cluster pod metrics from metrics API");
            }

            ILookup<string, PodMetricsItem>? metricsByNamespace = null;
            if (podMetricsList?.Items is { Length: > 0 } metricItems)
            {
                metricsByNamespace = metricItems.ToLookup(i => i.Metadata.Namespace);
            }

            var apiWriteParallelism = GetPositiveIntFromEnv("POD_METRICS_API_WRITE_PARALLELISM", defaultValue: 48);
            using var apiWriteGate = new SemaphoreSlim(apiWriteParallelism, apiWriteParallelism);

            var parallelism = GetPositiveIntFromEnv("POD_METRICS_NAMESPACE_PARALLELISM", defaultValue: 8);
            var parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = parallelism };

            await Parallel.ForEachAsync(namespacesToWatch, parallelOptions, async (ns, _) =>
            {
                try
                {
                    await CollectNamespaceAsync(
                        clientWrapper,
                        apiClient,
                        ns,
                        metricsByNamespace,
                        apiWriteGate,
                        isLogCollectionEnabled);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error collecting metrics for namespace '{Namespace}'", ns);
                }
            });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error during pod metrics collection");
        }
        finally
        {
            Log.Information("Pod metrics collection finished in {ElapsedSeconds:F3} s", sw.Elapsed.TotalSeconds);
        }
    }

    private static async Task CollectNamespaceAsync(
        IKubernetesClientWrapper clientWrapper,
        IMetricsApiClient apiClient,
        string ns,
        ILookup<string, PodMetricsItem>? metricsByNamespace,
        SemaphoreSlim apiWriteGate,
        bool isLogCollectionEnabled)
    {
        async Task GatedWriteAsync(Func<Task> send)
        {
            await apiWriteGate.WaitAsync();
            try
            {
                await send();
            }
            finally
            {
                apiWriteGate.Release();
            }
        }

        var pods = await clientWrapper.ListNamespacedPodAsync(ns);

        var podCpuLimits = new Dictionary<string, Dictionary<string, string>>();
        var podMemoryLimits = new Dictionary<string, Dictionary<string, string>>();
        var podNodeNames = new Dictionary<string, string>();
        var podStates = new Dictionary<string, string>();
        var podRestarts = new Dictionary<string, int>();
        var podAges = new Dictionary<string, long>();

        foreach (var pod in pods.Items)
        {
            var podName = pod.Metadata?.Name;
            if (string.IsNullOrEmpty(podName))
                continue;

            var containerCpuLimits = new Dictionary<string, string>();
            var containerMemoryLimits = new Dictionary<string, string>();
            if (pod.Spec?.Containers != null)
            {
                foreach (var container in pod.Spec.Containers)
                {
                    var cpuLimit = container.Resources?.Limits?.ContainsKey("cpu") == true
                        ? container.Resources.Limits["cpu"].ToString()
                        : container.Resources?.Requests?.ContainsKey("cpu") == true
                            ? container.Resources.Requests["cpu"].ToString()
                            : null;

                    if (cpuLimit != null)
                    {
                        containerCpuLimits[container.Name] = cpuLimit;
                    }

                    var memoryLimit = container.Resources?.Limits?.ContainsKey("memory") == true
                        ? container.Resources.Limits["memory"].ToString()
                        : container.Resources?.Requests?.ContainsKey("memory") == true
                            ? container.Resources.Requests["memory"].ToString()
                            : null;

                    if (memoryLimit != null)
                    {
                        containerMemoryLimits[container.Name] = memoryLimit;
                    }
                }
            }
            podCpuLimits[podName] = containerCpuLimits;
            podMemoryLimits[podName] = containerMemoryLimits;

            if (!string.IsNullOrWhiteSpace(pod.Spec?.NodeName))
            {
                podNodeNames[podName] = pod.Spec.NodeName;
            }

            if (!string.IsNullOrWhiteSpace(pod.Status?.Phase))
            {
                podStates[podName] = pod.Status.Phase;
            }

            var restartCount = pod.Status?.ContainerStatuses != null
                ? pod.Status.ContainerStatuses.Sum(cs => cs.RestartCount)
                : 0;
            podRestarts[podName] = restartCount;

            if (pod.Metadata?.CreationTimestamp != null)
            {
                var age = (long)(DateTime.UtcNow - pod.Metadata.CreationTimestamp.Value).TotalSeconds;
                podAges[podName] = age;
            }
        }

        foreach (var pod in pods.Items)
        {
            Log.Debug("Pod: {Namespace}/{PodName} - Phase: {Phase}",
                pod.Metadata.NamespaceProperty, pod.Metadata.Name, pod.Status.Phase);
        }

        var podsWithMetrics = new HashSet<string>();
        var metricWriteTasks = new List<Task>();
        var metricsForNs = metricsByNamespace?[ns];
        if (metricsForNs != null)
        {
            foreach (var item in metricsForNs)
            {
                podsWithMetrics.Add(item.Metadata.Name);
                Log.Debug("Pod: {Namespace}/{PodName} - Timestamp: {Timestamp}",
                    item.Metadata.Namespace, item.Metadata.Name, item.Timestamp);
                foreach (var container in item.Containers)
                {
                    var podName = item.Metadata.Name;
                    var cpuLimit = podCpuLimits.TryGetValue(podName, out var containerCpuLimits) &&
                                 containerCpuLimits.TryGetValue(container.Name, out var cpuLimitStr)
                        ? cpuLimitStr
                        : null;

                    var memoryLimit = podMemoryLimits.TryGetValue(podName, out var containerMemLimits) &&
                                      containerMemLimits.TryGetValue(container.Name, out var memoryLimitStr)
                        ? memoryLimitStr
                        : null;

                    var nodeName = podNodeNames.TryGetValue(podName, out var node)
                        ? node
                        : null;

                    var podState = podStates.TryGetValue(podName, out var state)
                        ? state
                        : null;

                    var restartCount = podRestarts.TryGetValue(podName, out var restarts)
                        ? restarts
                        : 0;

                    var podAge = podAges.TryGetValue(podName, out var age)
                        ? age
                        : (long?)null;

                    var cpuCores = ResourceFormatter.ParseCpuToCores(container.Usage.Cpu);
                    var memoryBytes = Utils.MemoryParser.ParseToBytes(container.Usage.Memory);
                    double? cpuLimitCores = null;

                    if (cpuLimit != null)
                    {
                        cpuLimitCores = ResourceFormatter.ParseCpuToCores(cpuLimit);
                    }

                    double? memoryLimitBytes = null;
                    if (memoryLimit != null)
                    {
                        memoryLimitBytes = Utils.MemoryParser.ParseToBytes(memoryLimit);
                    }

                    var itemNs = item.Metadata.Namespace;
                    var itemPod = item.Metadata.Name;
                    var containerName = container.Name;
                    var cpuU = container.Usage.Cpu;
                    var memU = container.Usage.Memory;
                    metricWriteTasks.Add(GatedWriteAsync(async () =>
                    {
                        try
                        {
                            await apiClient.WritePodMetricAsync(
                                itemNs,
                                itemPod,
                                containerName,
                                cpuCores,
                                cpuLimitCores,
                                memoryBytes,
                                memoryLimitBytes,
                                nodeName,
                                podState,
                                restartCount,
                                podAge);

                            var formattedCpu = ResourceFormatter.FormatCpu(cpuU, cpuLimit);
                            var formattedMemory = ResourceFormatter.FormatMemory(memU);
                            Log.Debug("Container: {Container} - CPU: {Cpu}, Memory: {Memory}",
                                containerName, formattedCpu, formattedMemory);
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex, "Error sending pod metric to API for {Namespace}/{PodName}/{Container}",
                                itemNs, itemPod, containerName);
                        }
                    }));
                }
            }
        }

        if (metricWriteTasks.Count > 0)
        {
            await Task.WhenAll(metricWriteTasks);
        }

        var zeroMetricWriteTasks = new List<Task>();
        foreach (var pod in pods.Items)
        {
            if (podsWithMetrics.Contains(pod.Metadata.Name))
                continue;

            var podState = podStates.TryGetValue(pod.Metadata.Name, out var state)
                ? state
                : null;

            var restartCount = podRestarts.TryGetValue(pod.Metadata.Name, out var restarts)
                ? restarts
                : 0;

            var podAge = podAges.TryGetValue(pod.Metadata.Name, out var age)
                ? age
                : (long?)null;

            var nodeName = podNodeNames.TryGetValue(pod.Metadata.Name, out var node)
                ? node
                : null;

            if (pod.Spec?.Containers != null)
            {
                foreach (var container in pod.Spec.Containers)
                {
                    var cpuLimit = podCpuLimits.TryGetValue(pod.Metadata.Name, out var containerCpuLimits) &&
                                 containerCpuLimits.TryGetValue(container.Name, out var cpuLimitStr)
                        ? cpuLimitStr
                        : null;

                    var memoryLimit = podMemoryLimits.TryGetValue(pod.Metadata.Name, out var containerMemLimits) &&
                                      containerMemLimits.TryGetValue(container.Name, out var memoryLimitStr)
                        ? memoryLimitStr
                        : null;

                    double? cpuLimitCores = null;
                    if (cpuLimit != null)
                    {
                        cpuLimitCores = ResourceFormatter.ParseCpuToCores(cpuLimit);
                    }

                    double? memoryLimitBytes = null;
                    if (memoryLimit != null)
                    {
                        memoryLimitBytes = Utils.MemoryParser.ParseToBytes(memoryLimit);
                    }

                    var podNs = pod.Metadata.NamespaceProperty;
                    var podName = pod.Metadata.Name;
                    var containerName = container.Name;
                    zeroMetricWriteTasks.Add(GatedWriteAsync(async () =>
                    {
                        try
                        {
                            await apiClient.WritePodMetricAsync(
                                podNs,
                                podName,
                                containerName,
                                0.0,
                                cpuLimitCores,
                                0.0,
                                memoryLimitBytes,
                                nodeName,
                                podState,
                                restartCount,
                                podAge);

                            Log.Debug("Stored metrics for non-running pod {Namespace}/{PodName}/{Container} - Phase: {Phase}",
                                podNs, podName, containerName, podState);
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex, "Error sending pod metric to API for {Namespace}/{PodName}/{Container}",
                                podNs, podName, containerName);
                        }
                    }));
                }
            }
        }

        if (zeroMetricWriteTasks.Count > 0)
        {
            await Task.WhenAll(zeroMetricWriteTasks);
        }

        if (isLogCollectionEnabled)
        {
            var tailLines = Environment.GetEnvironmentVariable("LOG_TAIL_LINES") ?? "100";
            var tailLinesInt = Convert.ToInt32(tailLines);
            var logTasks = new List<Task>();

            async Task FetchAndStoreLogAsync(V1Pod podRef, V1Container containerRef)
            {
                try
                {
                    var logContent = await clientWrapper.ReadNamespacedPodLogAsync(
                        podRef.Metadata.Name,
                        podRef.Metadata.NamespaceProperty,
                        containerRef.Name,
                        tailLines: tailLinesInt);

                    if (string.IsNullOrWhiteSpace(logContent))
                        return;

                    await GatedWriteAsync(async () =>
                    {
                        await apiClient.WritePodLogAsync(
                            podRef.Metadata.NamespaceProperty,
                            podRef.Metadata.Name,
                            containerRef.Name,
                            logContent);
                    });

                    Log.Debug("Stored logs for {Namespace}/{Pod}/{Container} ({LogLength} characters)",
                        podRef.Metadata.NamespaceProperty, podRef.Metadata.Name, containerRef.Name, logContent.Length);
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, "Error fetching logs for {Namespace}/{Pod}/{Container}",
                        podRef.Metadata.NamespaceProperty, podRef.Metadata.Name, containerRef.Name);
                }
            }

            foreach (var pod in pods.Items)
            {
                if (pod.Spec?.Containers == null)
                    continue;

                foreach (var container in pod.Spec.Containers)
                {
                    logTasks.Add(FetchAndStoreLogAsync(pod, container));
                }
            }

            if (logTasks.Count > 0)
            {
                await Task.WhenAll(logTasks);
            }
        }
        else
        {
            Log.Debug("Log collection is disabled (COLLECT_LOGS environment variable is not set to 'true')");
        }
    }

    private static int GetPositiveIntFromEnv(string name, int defaultValue)
    {
        var raw = Environment.GetEnvironmentVariable(name);
        return int.TryParse(raw, out var n) && n > 0 ? n : defaultValue;
    }
}
