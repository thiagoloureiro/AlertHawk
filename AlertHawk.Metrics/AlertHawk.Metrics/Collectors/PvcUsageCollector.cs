using System.Diagnostics;
using System.Text.Json;
using AlertHawk.Metrics.Models;
using k8s;
using Serilog;

namespace AlertHawk.Metrics.Collectors;

public static class PvcUsageCollector
{
    public static async Task CollectAsync(Kubernetes client, string[]? namespacesToWatch = null)
    {
        await CollectAsync(new KubernetesClientWrapper(client), null, null, namespacesToWatch);
    }

    /// <summary>
    /// Use config for node proxy (same auth as curl: Bearer token + CA). Use when client's Connect* returns 401.
    /// When apiClient is provided, PVC metrics are also sent to the API and stored in ClickHouse.
    /// Only pods in namespacesToWatch are reported (when namespacesToWatch is null or empty, all namespaces are included).
    /// </summary>
    public static async Task CollectAsync(Kubernetes client, KubernetesClientConfiguration config, IMetricsApiClient? apiClient = null, string[]? namespacesToWatch = null)
    {
        await CollectAsync(new KubernetesClientWrapper(client), config, apiClient, namespacesToWatch);
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static async Task CollectAsync(IKubernetesClientWrapper clientWrapper, KubernetesClientConfiguration? config = null, IMetricsApiClient? apiClient = null, string[]? namespacesToWatch = null)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            var watchSet = namespacesToWatch != null && namespacesToWatch.Length > 0
                ? new HashSet<string>(namespacesToWatch, StringComparer.OrdinalIgnoreCase)
                : null;

            Log.Information("Collecting PVC/volume usage from nodes (namespaces: {Namespaces})...",
                watchSet != null ? string.Join(", ", watchSet) : "all");

            var nodes = await clientWrapper.ListNodeAsync();
            if (nodes?.Items == null || nodes.Items.Count == 0)
            {
                Log.Information("No nodes found.");
                return;
            }

            var nodeItems = nodes.Items.Where(n => !string.IsNullOrEmpty(n.Metadata?.Name)).ToList();
            var parallelism = GetPositiveIntFromEnv("PVC_NODE_COLLECTION_PARALLELISM", defaultValue: 8);
            var parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = parallelism };

            await Parallel.ForEachAsync(nodeItems, parallelOptions, async (node, cancellationToken) =>
            {
                var nodeName = node.Metadata!.Name!;
                try
                {
                    var json = config != null
                        ? await NodeProxyHttpHelper.GetNodeStatsSummaryAsync(config, nodeName, cancellationToken)
                        : await clientWrapper.GetNodeStatsSummaryAsync(nodeName, cancellationToken);
                    if (string.IsNullOrWhiteSpace(json))
                    {
                        Log.Debug("Empty stats/summary for node {NodeName}", nodeName);
                        return;
                    }

                    var summary = JsonSerializer.Deserialize<StatsSummary>(json, JsonOptions);
                    if (summary?.Pods == null)
                        return;

                    foreach (var pod in summary.Pods)
                    {
                        var ns = pod.PodRef?.Namespace ?? "";
                        var podName = pod.PodRef?.Name ?? "";

                        if (watchSet != null && !watchSet.Contains(ns))
                            continue;

                        foreach (var vol in pod.Volume)
                        {
                            if (vol.PvcRef == null)
                                continue;

                            var used = vol.UsedBytes ?? 0;
                            var available = vol.AvailableBytes ?? 0;
                            var capacity = vol.CapacityBytes ?? 0;
                            var pvcRef = $"{vol.PvcRef.Namespace}/{vol.PvcRef.Name}";

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
            });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error during PVC usage collection");
        }
        finally
        {
            Log.Information("PVC/volume usage collection finished in {ElapsedSeconds:F3} s", sw.Elapsed.TotalSeconds);
        }
    }

    private static int GetPositiveIntFromEnv(string name, int defaultValue)
    {
        var raw = Environment.GetEnvironmentVariable(name);
        return int.TryParse(raw, out var n) && n > 0 ? n : defaultValue;
    }
}
