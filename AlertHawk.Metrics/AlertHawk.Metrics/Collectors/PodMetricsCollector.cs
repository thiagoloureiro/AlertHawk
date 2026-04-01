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

        // Check if log collection is enabled (once per collection cycle)
        var collectLogs = Environment.GetEnvironmentVariable("COLLECT_LOGS");
        var isLogCollectionEnabled = !string.IsNullOrWhiteSpace(collectLogs) && 
                                      collectLogs.Equals("true", StringComparison.OrdinalIgnoreCase);

        try
        {
            Log.Information("Collecting pod metrics...");

            foreach (var ns in namespacesToWatch)
            {
                try
                {
                    var pods = await clientWrapper.ListNamespacedPodAsync(ns);

                    // Build dictionaries of pod name -> per-container resource limits (string form from the API)
                    var podCpuLimits = new Dictionary<string, Dictionary<string, string>>();
                    var podMemoryLimits = new Dictionary<string, Dictionary<string, string>>();
                    // Build a dictionary of pod name -> node name
                    var podNodeNames = new Dictionary<string, string>();
                    // Build a dictionary of pod name -> pod state (Phase)
                    var podStates = new Dictionary<string, string>();
                    // Build a dictionary of pod name -> restart count
                    var podRestarts = new Dictionary<string, int>();
                    // Build a dictionary of pod name -> age in seconds
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
                                // Prefer limit over request, and convert to string format
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
                        
                        // Store node name for this pod
                        if (!string.IsNullOrWhiteSpace(pod.Spec?.NodeName))
                        {
                            podNodeNames[podName] = pod.Spec.NodeName;
                        }
                        
                        // Store pod state (Phase)
                        if (!string.IsNullOrWhiteSpace(pod.Status?.Phase))
                        {
                            podStates[podName] = pod.Status.Phase;
                        }
                        
                        // Calculate total restart count for all containers
                        var restartCount = pod.Status?.ContainerStatuses != null
                            ? pod.Status.ContainerStatuses.Sum(cs => cs.RestartCount)
                            : 0;
                        podRestarts[podName] = restartCount;
                        
                        // Calculate pod age in seconds
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

                    var response = await clientWrapper.ListClusterCustomObjectAsync(
                        group: "metrics.k8s.io",
                        version: "v1beta1",
                        plural: "pods");

                    var jsonString = JsonSerializer.Serialize(response);
                    var podMetricsList = JsonSerializer.Deserialize<PodMetricsList>(jsonString, jsonOptions);

                    // Build a set of pod names that have metrics from the metrics API
                    var podsWithMetrics = new HashSet<string>();
                    if (podMetricsList != null)
                    {
                        Log.Debug("Found {Count} pod metrics", podMetricsList.Items.Length);
                        foreach (var item in podMetricsList.Items)
                        {
                            // Only show metrics for pods in current namespace
                            if (item.Metadata.Namespace != ns)
                                continue;

                            podsWithMetrics.Add(item.Metadata.Name);
                            Log.Debug("Pod: {Namespace}/{PodName} - Timestamp: {Timestamp}", 
                                item.Metadata.Namespace, item.Metadata.Name, item.Timestamp);
                            foreach (var container in item.Containers)
                            {
                                // Get CPU / memory limits for this container
                                var cpuLimit = podCpuLimits.TryGetValue(item.Metadata.Name, out var containerCpuLimits) &&
                                             containerCpuLimits.TryGetValue(container.Name, out var cpuLimitStr)
                                    ? cpuLimitStr
                                    : null;

                                var memoryLimit = podMemoryLimits.TryGetValue(item.Metadata.Name, out var containerMemLimits) &&
                                                  containerMemLimits.TryGetValue(container.Name, out var memoryLimitStr)
                                    ? memoryLimitStr
                                    : null;

                                // Get node name for this pod
                                var nodeName = podNodeNames.TryGetValue(item.Metadata.Name, out var node)
                                    ? node
                                    : null;

                                // Get pod state
                                var podState = podStates.TryGetValue(item.Metadata.Name, out var state)
                                    ? state
                                    : null;

                                // Get restart count
                                var restartCount = podRestarts.TryGetValue(item.Metadata.Name, out var restarts)
                                    ? restarts
                                    : 0;

                                // Get pod age in seconds
                                var podAge = podAges.TryGetValue(item.Metadata.Name, out var age)
                                    ? age
                                    : (long?)null;

                                // Write metrics to ClickHouse
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

                                try
                                {
                                    await apiClient.WritePodMetricAsync(
                                        item.Metadata.Namespace,
                                        item.Metadata.Name,
                                        container.Name,
                                        cpuCores,
                                        cpuLimitCores,
                                        memoryBytes,
                                        memoryLimitBytes,
                                        nodeName,
                                        podState,
                                        restartCount,
                                        podAge);
                                }
                                catch (Exception ex)
                                {
                                    Log.Error(ex, "Error sending pod metric to API for {Namespace}/{PodName}/{Container}", 
                                        item.Metadata.Namespace, item.Metadata.Name, container.Name);
                                }

                                var formattedCpu = ResourceFormatter.FormatCpu(container.Usage.Cpu, cpuLimit);
                                var formattedMemory = ResourceFormatter.FormatMemory(container.Usage.Memory);
                                Log.Debug("Container: {Container} - CPU: {Cpu}, Memory: {Memory}", 
                                    container.Name, formattedCpu, formattedMemory);
                            }
                        }
                    }

                    // Process pods that don't have metrics from the metrics API (e.g., Pending, Failed, Succeeded)
                    foreach (var pod in pods.Items)
                    {
                        // Skip if we already processed this pod from the metrics API
                        if (podsWithMetrics.Contains(pod.Metadata.Name))
                            continue;

                        // Get pod state
                        var podState = podStates.TryGetValue(pod.Metadata.Name, out var state)
                            ? state
                            : null;

                        // Get restart count
                        var restartCount = podRestarts.TryGetValue(pod.Metadata.Name, out var restarts)
                            ? restarts
                            : 0;

                        // Get pod age in seconds
                        var podAge = podAges.TryGetValue(pod.Metadata.Name, out var age)
                            ? age
                            : (long?)null;

                        // Get node name for this pod
                        var nodeName = podNodeNames.TryGetValue(pod.Metadata.Name, out var node)
                            ? node
                            : null;

                        // Process each container in the pod
                        if (pod.Spec?.Containers != null)
                        {
                            foreach (var container in pod.Spec.Containers)
                            {
                                // Get CPU / memory limits for this container
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

                                try
                                {
                                    // Write metrics with 0 CPU and memory for non-running pods
                                    await apiClient.WritePodMetricAsync(
                                        pod.Metadata.NamespaceProperty,
                                        pod.Metadata.Name,
                                        container.Name,
                                        0.0, // CPU usage
                                        cpuLimitCores,
                                        0.0, // Memory usage
                                        memoryLimitBytes,
                                        nodeName,
                                        podState,
                                        restartCount,
                                        podAge);
                                    
                                    Log.Debug("Stored metrics for non-running pod {Namespace}/{PodName}/{Container} - Phase: {Phase}", 
                                        pod.Metadata.NamespaceProperty, pod.Metadata.Name, container.Name, podState);
                                }
                                catch (Exception ex)
                                {
                                    Log.Error(ex, "Error sending pod metric to API for {Namespace}/{PodName}/{Container}", 
                                        pod.Metadata.NamespaceProperty, pod.Metadata.Name, container.Name);
                                }
                            }
                        }
                    }

                    // Fetch and store pod logs (only if COLLECT_LOGS is enabled)
                    if (isLogCollectionEnabled)
                    {
                        var tailLines = Environment.GetEnvironmentVariable("LOG_TAIL_LINES") ?? "100";
                        foreach (var pod in pods.Items)
                        {
                            try
                            {
                                if (pod.Spec?.Containers != null)
                                {
                                    foreach (var container in pod.Spec.Containers)
                                    {
                                        try
                                        {
                                            var logContent = await clientWrapper.ReadNamespacedPodLogAsync(
                                                pod.Metadata.Name,
                                                pod.Metadata.NamespaceProperty,
                                                container.Name,
                                                tailLines: Convert.ToInt32(tailLines));

                                            if (!string.IsNullOrWhiteSpace(logContent))
                                            {
                                                await apiClient.WritePodLogAsync(
                                                    pod.Metadata.NamespaceProperty,
                                                    pod.Metadata.Name,
                                                    container.Name,
                                                    logContent);
                                                
                                                Log.Debug("Stored logs for {Namespace}/{Pod}/{Container} ({LogLength} characters)", 
                                                    pod.Metadata.NamespaceProperty, pod.Metadata.Name, container.Name, logContent.Length);
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            Log.Warning(ex, "Error fetching logs for {Namespace}/{Pod}/{Container}", 
                                                pod.Metadata.NamespaceProperty, pod.Metadata.Name, container.Name);
                                        }
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                Log.Warning(ex, "Error processing logs for pod {Namespace}/{Pod}", 
                                    pod.Metadata.NamespaceProperty, pod.Metadata.Name);
                            }
                        }
                    }
                    else
                    {
                        Log.Debug("Log collection is disabled (COLLECT_LOGS environment variable is not set to 'true')");
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

