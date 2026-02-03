using System.Text.Json;
using AlertHawk.Metrics.Models;
using k8s;
using Serilog;

namespace AlertHawk.Metrics.Collectors;

public static class PvcUsageCollector
{
    public static async Task CollectAsync(Kubernetes client)
    {
        await CollectAsync(new KubernetesClientWrapper(client));
    }

    /// <summary>
    /// Use config for node proxy (same auth as curl: Bearer token + CA). Use when client's Connect* returns 401.
    /// When apiClient is provided, PVC metrics are also sent to the API and stored in ClickHouse.
    /// </summary>
    public static async Task CollectAsync(Kubernetes client, KubernetesClientConfiguration config, IMetricsApiClient? apiClient = null)
    {
        await CollectAsync(new KubernetesClientWrapper(client), config, apiClient);
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static async Task CollectAsync(IKubernetesClientWrapper clientWrapper, KubernetesClientConfiguration? config = null, IMetricsApiClient? apiClient = null)
    {
        try
        {
            Log.Information("Collecting PVC/volume usage from nodes...");
            Console.WriteLine("Namespace\tPod\tPVC/Volume\tVolumeName\tUsedBytes\tAvailableBytes\tCapacityBytes");

            var nodes = await clientWrapper.ListNodeAsync();
            if (nodes?.Items == null || nodes.Items.Count == 0)
            {
                Log.Information("No nodes found.");
                return;
            }

            foreach (var node in nodes.Items)
            {
                var nodeName = node.Metadata?.Name;
                if (string.IsNullOrEmpty(nodeName))
                    continue;

                try
                {
                    var json = config != null
                        ? await NodeProxyHttpHelper.GetNodeStatsSummaryAsync(config, nodeName)
                        : await clientWrapper.GetNodeStatsSummaryAsync(nodeName);
                    if (string.IsNullOrWhiteSpace(json))
                    {
                        Log.Debug("Empty stats/summary for node {NodeName}", nodeName);
                        continue;
                    }

                    var summary = JsonSerializer.Deserialize<StatsSummary>(json, JsonOptions);
                    if (summary?.Pods == null)
                        continue;

                    foreach (var pod in summary.Pods)
                    {
                        var ns = pod.PodRef?.Namespace ?? "";
                        var podName = pod.PodRef?.Name ?? "";

                        foreach (var vol in pod.Volume)
                        {
                            // Only report volumes backed by a PersistentVolumeClaim (actual PVC usage)
                            if (vol.PvcRef == null)
                                continue;

                            var used = vol.UsedBytes ?? 0;
                            var available = vol.AvailableBytes ?? 0;
                            var capacity = vol.CapacityBytes ?? 0;
                            var pvcRef = $"{vol.PvcRef.Namespace}/{vol.PvcRef.Name}";

                            Console.WriteLine(
                                $"{ns}\t{podName}\t{pvcRef}\t{vol.Name}\t{used}\t{available}\t{capacity}");

                            if (apiClient != null)
                            {
                                try
                                {
                                    await apiClient.WritePvcMetricAsync(
                                        ns,
                                        podName,
                                        vol.PvcRef.Namespace ?? "",
                                        vol.PvcRef.Name ?? "",
                                        vol.Name,
                                        used,
                                        available,
                                        capacity);
                                }
                                catch (Exception ex)
                                {
                                    Log.Warning(ex, "Failed to send PVC metric to API for {Namespace}/{Pod} {PvcRef}", ns, podName, pvcRef);
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, "Failed to get stats/summary for node {NodeName}", nodeName);
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error during PVC usage collection");
        }
    }
}
