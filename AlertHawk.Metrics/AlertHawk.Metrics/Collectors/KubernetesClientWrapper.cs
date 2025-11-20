using k8s;
using k8s.Models;

namespace AlertHawk.Metrics.Collectors;

/// <summary>
/// Default implementation of IKubernetesClientWrapper that wraps the real Kubernetes client.
/// </summary>
public class KubernetesClientWrapper : IKubernetesClientWrapper
{
    private readonly Kubernetes _kubernetes;

    public KubernetesClientWrapper(Kubernetes kubernetes)
    {
        _kubernetes = kubernetes;
    }

    public async Task<V1PodList> ListNamespacedPodAsync(string namespaceParameter)
    {
        return await _kubernetes.CoreV1.ListNamespacedPodAsync(namespaceParameter);
    }

    public async Task<object> ListClusterCustomObjectAsync(string group, string version, string plural)
    {
        return await _kubernetes.CustomObjects.ListClusterCustomObjectAsync(group, version, plural);
    }

    public async Task<V1NodeList> ListNodeAsync()
    {
        return await _kubernetes.CoreV1.ListNodeAsync();
    }
}

