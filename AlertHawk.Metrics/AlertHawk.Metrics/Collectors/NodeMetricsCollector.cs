using System.Text.Json;
using AlertHawk.Metrics;
using k8s;
using Serilog;

namespace AlertHawk.Metrics.Collectors;

public static class NodeMetricsCollector
{
    public static async Task CollectAsync(
        Kubernetes client,
        MetricsApiClient apiClient)
    {
        await CollectAsync(new KubernetesClientWrapper(client), apiClient);
    }

    public static async Task CollectAsync(
        IKubernetesClientWrapper clientWrapper,
        IMetricsApiClient apiClient)
    {
        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        try
        {
            Log.Information("Collecting node metrics...");

            // Fetch Kubernetes version and cloud provider once per collection cycle
            string? kubernetesVersion = null;
            string? cloudProvider = null;
            
            try
            {
                var version = await clientWrapper.GetVersionAsync();
                kubernetesVersion = version.GitVersion;
                Log.Debug("Kubernetes version: {Version}", kubernetesVersion);
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Could not fetch Kubernetes version");
            }

            try
            {
                cloudProvider = await clientWrapper.DetectCloudProviderAsync();
                Log.Debug("Cloud provider: {Provider}", cloudProvider);
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Could not detect cloud provider");
            }

            // Get node list to retrieve capacity information and conditions
            var nodes = await clientWrapper.ListNodeAsync();
            var nodeCapacities = new Dictionary<string, (double cpuCores, double memoryBytes)>();
            var nodeConditions = new Dictionary<string, (bool? isReady, bool? hasMemoryPressure, bool? hasDiskPressure, bool? hasPidPressure)>();
            var nodeInfo = new Dictionary<string, (string? architecture, string? operatingSystem)>();
            var nodeLabels = new Dictionary<string, (string? region, string? instanceType)>();

            foreach (var node in nodes.Items)
            {
                var cpuCapacity = 0.0;
                var memoryCapacity = 0.0;
                bool? isReady = null;
                bool? hasMemoryPressure = null;
                bool? hasDiskPressure = null;
                bool? hasPidPressure = null;
                string? architecture = null;
                string? operatingSystem = null;

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
                        memoryCapacity = Utils.MemoryParser.ParseToBytes(memoryStr);
                    }
                }

                // Extract node conditions
                if (node.Status?.Conditions != null)
                {
                    foreach (var condition in node.Status.Conditions)
                    {
                        var isTrue = condition.Status?.Equals("True", StringComparison.OrdinalIgnoreCase) ?? false;
                        
                        switch (condition.Type?.ToLowerInvariant())
                        {
                            case "ready":
                                isReady = isTrue;
                                break;
                            case "memorypressure":
                                hasMemoryPressure = isTrue;
                                break;
                            case "diskpressure":
                                hasDiskPressure = isTrue;
                                break;
                            case "pidpressure":
                                hasPidPressure = isTrue;
                                break;
                        }
                    }
                }

                // Extract architecture and operating system
                if (node.Status?.NodeInfo != null)
                {
                    architecture = node.Status.NodeInfo.Architecture;
                    operatingSystem = node.Status.NodeInfo.OperatingSystem;
                }

                // Extract labels
                string? region = null;
                string? instanceType = null;
                if (node.Metadata?.Labels != null)
                {
                    // Extract topology.kubernetes.io/region
                    if (node.Metadata.Labels.TryGetValue("topology.kubernetes.io/region", out var regionValue))
                    {
                        region = regionValue;
                    }
                    
                    // Extract beta.kubernetes.io/instance-type (or topology.kubernetes.io/zone if region is not found)
                    if (node.Metadata.Labels.TryGetValue("beta.kubernetes.io/instance-type", out var instanceTypeValue))
                    {
                        instanceType = instanceTypeValue;
                    }
                    // Also check for node.kubernetes.io/instance-type (newer label format)
                    else if (node.Metadata.Labels.TryGetValue("node.kubernetes.io/instance-type", out var instanceTypeValue2))
                    {
                        instanceType = instanceTypeValue2;
                    }
                }

                if (node.Metadata?.Name != null)
                {
                    nodeCapacities[node.Metadata.Name] = (cpuCapacity, memoryCapacity);
                    nodeConditions[node.Metadata.Name] = (isReady, hasMemoryPressure, hasDiskPressure, hasPidPressure);
                    nodeInfo[node.Metadata.Name] = (architecture, operatingSystem);
                    nodeLabels[node.Metadata.Name] = (region, instanceType);
                }
            }

            // Get node metrics from metrics API
            var nodeMetricsResponse = await clientWrapper.ListClusterCustomObjectAsync(
                group: "metrics.k8s.io",
                version: "v1beta1",
                plural: "nodes");

            var nodeMetricsJson = JsonSerializer.Serialize(nodeMetricsResponse);
            var nodeMetricsList = JsonSerializer.Deserialize<NodeMetricsList>(nodeMetricsJson, jsonOptions);

            if (nodeMetricsList != null)
            {
                Log.Debug("Found {Count} node metrics", nodeMetricsList.Items.Length);
                foreach (var nodeMetric in nodeMetricsList.Items)
                {
                    var nodeName = nodeMetric.Metadata.Name;
                    var cpuUsageCores = ResourceFormatter.ParseCpuToCores(nodeMetric.Usage.Cpu);
                    var memoryUsageBytes = Utils.MemoryParser.ParseToBytes(nodeMetric.Usage.Memory);

                    // Get capacity for this node
                    var (cpuCapacityCores, memoryCapacityBytes) = nodeCapacities.TryGetValue(nodeName, out var capacity)
                        ? capacity
                        : (0.0, 0.0);

                    // Get conditions for this node
                    var (isReady, hasMemoryPressure, hasDiskPressure, hasPidPressure) = nodeConditions.TryGetValue(nodeName, out var conditions)
                        ? conditions
                        : ((bool?)null, (bool?)null, (bool?)null, (bool?)null);

                    // Get architecture and OS for this node
                    var (architecture, operatingSystem) = nodeInfo.TryGetValue(nodeName, out var info)
                        ? info
                        : ((string?)null, (string?)null);

                    // Get labels for this node
                    var (region, instanceType) = nodeLabels.TryGetValue(nodeName, out var labels)
                        ? labels
                        : ((string?)null, (string?)null);

                    if (memoryUsageBytes > 0)
                    {
                        try
                        {
                            await apiClient.WriteNodeMetricAsync(
                                nodeName,
                                cpuUsageCores,
                                cpuCapacityCores,
                                memoryUsageBytes,
                                memoryCapacityBytes,
                                kubernetesVersion,
                                cloudProvider,
                                isReady,
                                hasMemoryPressure,
                                hasDiskPressure,
                                hasPidPressure,
                                architecture,
                                operatingSystem,
                                region,
                                instanceType);

                            var cpuPercent = cpuCapacityCores > 0 ? (cpuUsageCores / cpuCapacityCores * 100) : 0;
                            var memoryPercent = memoryCapacityBytes > 0 ? (memoryUsageBytes / memoryCapacityBytes * 100) : 0;

                            Log.Debug("Node: {NodeName} - CPU: {CpuUsage:F2}/{CpuCapacity:F2} cores ({CpuPercent:F1}%), Memory: {Memory} ({MemoryPercent:F1}%)", 
                                nodeName, cpuUsageCores, cpuCapacityCores, cpuPercent, 
                                ResourceFormatter.FormatMemory(nodeMetric.Usage.Memory), memoryPercent);
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex, "Error sending node metric to API for node {NodeName}", nodeName);
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error during node metrics collection");
        }
    }
}

