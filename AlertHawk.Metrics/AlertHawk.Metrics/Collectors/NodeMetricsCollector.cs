using System.Text.Json;
using AlertHawk.Metrics;
using k8s;

namespace AlertHawk.Metrics.Collectors;

public static class NodeMetricsCollector
{
    public static async Task CollectAsync(
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
                        memoryCapacity = Utils.MemoryParser.ParseToBytes(memoryStr);
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
}

