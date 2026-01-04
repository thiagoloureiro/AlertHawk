using k8s;
using k8s.Models;
using Serilog;

namespace AlertHawk.Metrics.Collectors;

public static class EventsCollector
{
    // Track last collection timestamp per namespace to only fetch new/modified events
    private static readonly Dictionary<string, DateTime> LastCollectionTime = new();

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
        try
        {
            Log.Information("Collecting Kubernetes events...");

            foreach (var ns in namespacesToWatch)
            {
                try
                {
                    var events = await clientWrapper.ListNamespacedEventAsync(ns);
                    
                    // Get the last collection time for this namespace
                    var lastCollection = LastCollectionTime.TryGetValue(ns, out var lastTime) 
                        ? lastTime 
                        : DateTime.MinValue;

                    var newEventsCount = 0;
                    var updatedEventsCount = 0;

                    foreach (var evt in events.Items)
                    {
                        // Only process events that are new or have been updated since last collection
                        // Events are considered new/updated if:
                        // 1. They were first seen after last collection, OR
                        // 2. They were last modified after last collection
                        var firstSeen = evt.FirstTimestamp ?? DateTime.MinValue;
                        var lastModified = evt.LastTimestamp ?? evt.FirstTimestamp ?? DateTime.MinValue;
                        
                        // Use the later of firstSeen or lastModified to determine if event is new/updated
                        var eventTime = firstSeen > lastModified ? firstSeen : lastModified;

                        if (eventTime > lastCollection)
                        {
                            // Determine if this is a new event or an update
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
                                {
                                    newEventsCount++;
                                }
                                else
                                {
                                    updatedEventsCount++;
                                }
                            }
                            catch (Exception ex)
                            {
                                Log.Error(ex, "Error sending event to API for {Namespace}/{EventName}", 
                                    ns, evt.Metadata?.Name);
                            }
                        }
                    }

                    // Update last collection time for this namespace
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
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error during events collection");
        }
    }
}

