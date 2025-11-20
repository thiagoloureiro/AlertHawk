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

            // Get node list to retrieve capacity information
            var nodes = await clientWrapper.ListNodeAsync();
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
                        memoryCapacity = Utils.MemoryParser.ParseToBytes(memoryStr);
                    }
                }

                if (node.Metadata?.Name != null)
                {
                    nodeCapacities[node.Metadata.Name] = (cpuCapacity, memoryCapacity);
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

