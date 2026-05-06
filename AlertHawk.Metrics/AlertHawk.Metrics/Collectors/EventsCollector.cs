using System.Collections.Concurrent;
using System.Diagnostics;
using k8s;
using k8s.Models;
using Serilog;

namespace AlertHawk.Metrics.Collectors;

public static class EventsCollector
{
    // Track last collection timestamp per namespace to only fetch new/modified events
    private static readonly ConcurrentDictionary<string, DateTime> LastCollectionTime = new();

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
        var sw = Stopwatch.StartNew();
        try
        {
            Log.Information("Collecting Kubernetes events...");

            var parallelism = GetPositiveIntFromEnv("EVENTS_NAMESPACE_PARALLELISM", defaultValue: 8);
            var parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = parallelism };

            await Parallel.ForEachAsync(namespacesToWatch, parallelOptions, async (ns, _) =>
            {
                try
                {
                    var events = await clientWrapper.ListNamespacedEventAsync(ns);

                    var lastCollection = LastCollectionTime.TryGetValue(ns, out var lastTime)
                        ? lastTime
                        : DateTime.MinValue;

                    var newEventsCount = 0;
                    var updatedEventsCount = 0;

                    foreach (var evt in events.Items)
                    {
                        var firstSeen = evt.FirstTimestamp ?? DateTime.MinValue;
                        var lastModified = evt.LastTimestamp ?? evt.FirstTimestamp ?? DateTime.MinValue;

                        var eventTime = firstSeen > lastModified ? firstSeen : lastModified;

                        if (eventTime > lastCollection)
                        {
                            var isNew = firstSeen > lastCollection;

                            try
                            {
                                await apiClient.WriteKubernetesEventAsync(
                                    evt.Metadata?.NamespaceProperty ?? ns,
                                    evt.Metadata?.Name ?? string.Empty,
                                    evt.Metadata?.Uid ?? string.Empty,
                                    evt.InvolvedObject?.Kind ?? string.Empty,
                                    evt.InvolvedObject?.Name ?? string.Empty,
                                    evt.InvolvedObject?.NamespaceProperty ?? ns,
                                    evt.Type ?? string.Empty,
                                    evt.Reason ?? string.Empty,
                                    evt.Message ?? string.Empty,
                                    evt.Source?.Component ?? string.Empty,
                                    evt.Count ?? 1,
                                    firstSeen != DateTime.MinValue ? firstSeen : null,
                                    lastModified != DateTime.MinValue ? lastModified : null);

                                if (isNew)
                                    newEventsCount++;
                                else
                                    updatedEventsCount++;
                            }
                            catch (Exception ex)
                            {
                                Log.Error(ex, "Error sending event to API for {Namespace}/{EventName}",
                                    ns, evt.Metadata?.Name);
                            }
                        }
                    }

                    LastCollectionTime[ns] = DateTime.UtcNow;

                    if (newEventsCount > 0 || updatedEventsCount > 0)
                    {
                        Log.Information("Collected {NewCount} new and {UpdatedCount} updated events from namespace '{Namespace}'",
                            newEventsCount, updatedEventsCount, ns);
                    }
                    else
                    {
                        Log.Debug("No new or updated events in namespace '{Namespace}'", ns);
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error collecting events for namespace '{Namespace}'", ns);
                }
            });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error during events collection");
        }
        finally
        {
            Log.Information("Kubernetes events collection finished in {ElapsedSeconds:F3} s", sw.Elapsed.TotalSeconds);
        }
    }

    private static int GetPositiveIntFromEnv(string name, int defaultValue)
    {
        var raw = Environment.GetEnvironmentVariable(name);
        return int.TryParse(raw, out var n) && n > 0 ? n : defaultValue;
    }
}

