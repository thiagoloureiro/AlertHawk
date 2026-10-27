using k8s;
using k8s.Models;

namespace AlertHawk.Monitoring.Infrastructure.MonitorRunner;

public interface IK8sNodeClient
{
    Task<IList<V1Node>> ListNodesAsync(string kubeConfigPath);
}

public class K8sNodeClient : IK8sNodeClient
{
    public async Task<IList<V1Node>> ListNodesAsync(string kubeConfigPath)
    {
        var config = KubernetesClientConfiguration.BuildConfigFromConfigFile(kubeConfigPath);
        using var client = new Kubernetes(config);
        var nodes = await client.CoreV1.ListNodeAsync();
        return nodes.Items;
    }
}
