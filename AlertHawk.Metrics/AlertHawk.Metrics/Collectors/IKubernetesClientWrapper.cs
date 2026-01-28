using k8s.Models;

namespace AlertHawk.Metrics.Collectors;

/// <summary>
/// Wrapper interface for Kubernetes client operations to enable testing.
/// </summary>
public interface IKubernetesClientWrapper
{
    Task<V1PodList> ListNamespacedPodAsync(string namespaceParameter);
    Task<object> ListClusterCustomObjectAsync(string group, string version, string plural);
    Task<V1NodeList> ListNodeAsync();
    Task<VersionInfo> GetVersionAsync();
    Task<string> DetectCloudProviderAsync();
    Task<string> ReadNamespacedPodLogAsync(string name, string namespaceParameter, string? container = null, int? tailLines = null);
    Task<Corev1EventList> ListNamespacedEventAsync(string namespaceParameter);
    Task<string> GetNodeStatsSummaryAsync(string nodeName);
}

