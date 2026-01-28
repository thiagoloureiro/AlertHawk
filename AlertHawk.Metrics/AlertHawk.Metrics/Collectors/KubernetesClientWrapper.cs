using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using System.Text;
using k8s;
using k8s.Models;
using System.Net;

namespace AlertHawk.Metrics.Collectors;

/// <summary>
/// Default implementation of IKubernetesClientWrapper that wraps the real Kubernetes client.
/// </summary>
[ExcludeFromCodeCoverage]
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

    public async Task<VersionInfo> GetVersionAsync()
    {
        var version = await _kubernetes.Version.GetCodeAsync();
        return version;
    }

    public async Task<string> DetectCloudProviderAsync()
    {
        var nodes = await ListNodeAsync();
        
        if (nodes?.Items == null || nodes.Items.Count == 0)
        {
            return "Unknown";
        }

        // Check the first node for provider information
        var firstNode = nodes.Items[0];
        
        // Check provider ID (most reliable method)
        if (!string.IsNullOrEmpty(firstNode.Spec?.ProviderID))
        {
            var providerId = firstNode.Spec.ProviderID.ToLowerInvariant();
            if (providerId.StartsWith("azure://"))
                return "AKS";
            if (providerId.StartsWith("aws://") || providerId.Contains("eks"))
                return "EKS";
            if (providerId.StartsWith("gce://") || providerId.Contains("gke"))
                return "GKE";
        }

        // Check node labels (fallback method)
        if (firstNode.Metadata?.Labels != null)
        {
            var labels = firstNode.Metadata.Labels;
            
            // Check for Azure (AKS)
            if (labels.ContainsKey("kubernetes.azure.com/role") || 
                labels.ContainsKey("kubernetes.azure.com/cluster") ||
                labels.Any(kvp => kvp.Key.Contains("azure", StringComparison.OrdinalIgnoreCase)))
            {
                return "AKS";
            }
            
            // Check for AWS (EKS)
            if (labels.ContainsKey("eks.amazonaws.com/nodegroup") ||
                labels.ContainsKey("alpha.eksctl.io/nodegroup-name") ||
                labels.Any(kvp => kvp.Key.Contains("eks", StringComparison.OrdinalIgnoreCase) || 
                                 kvp.Key.Contains("amazonaws", StringComparison.OrdinalIgnoreCase)))
            {
                return "EKS";
            }
            
            // Check for GCP (GKE)
            if (labels.ContainsKey("cloud.google.com/gke-nodepool") ||
                labels.ContainsKey("cloud.google.com/gke-os-distribution") ||
                labels.Any(kvp => kvp.Key.Contains("gke", StringComparison.OrdinalIgnoreCase) || 
                                 kvp.Key.Contains("google", StringComparison.OrdinalIgnoreCase)))
            {
                return "GKE";
            }
        }

        return "Other";
    }

    public async Task<string> ReadNamespacedPodLogAsync(string name, string namespaceParameter, string? container = null, int? tailLines = null)
    {
        try
        {
            // ReadNamespacedPodLogAsync returns a Stream, so we need to read it
            var stream = await _kubernetes.CoreV1.ReadNamespacedPodLogAsync(
                name: name,
                namespaceParameter: namespaceParameter,
                container: container,
                tailLines: tailLines);
            
            if (stream == null)
            {
                return string.Empty;
            }

            using var reader = new StreamReader(stream, Encoding.UTF8);
            return await reader.ReadToEndAsync();
        }
        catch (Exception)
        {
            // Return empty string if log cannot be read (pod might not be running, container might not exist, etc.)
            return string.Empty;
        }
    }

    public async Task<Corev1EventList> ListNamespacedEventAsync(string namespaceParameter)
    {
        return await _kubernetes.CoreV1.ListNamespacedEventAsync(namespaceParameter);
    }

    public async Task<string> GetNodeStatsSummaryAsync(string nodeName)
    {
        try
        {
            // Access kubelet stats/summary endpoint through the API server proxy
            // The path is: /api/v1/nodes/{nodeName}/proxy/stats/summary
            // We'll use a raw HTTP call with the Kubernetes client's base URI
            using var httpClient = new HttpClient();
            httpClient.BaseAddress = _kubernetes.BaseUri;
            
            // Make the request to the proxy endpoint
            var response = await httpClient.GetAsync($"/api/v1/nodes/{nodeName}/proxy/stats/summary");
            response.EnsureSuccessStatusCode();
            
            return await response.Content.ReadAsStringAsync();
        }
        catch (Exception)
        {
            // Return empty JSON if stats cannot be read (may require authentication or proxy may not be available)
            return "{}";
        }
    }
}

