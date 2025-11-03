using k8s;

var config = KubernetesClientConfiguration.InClusterConfig();
var client = new Kubernetes(config);

var pods = await client.CoreV1.ListPodForAllNamespacesAsync();

foreach (var pod in pods.Items)
{
    Console.WriteLine($"{pod.Metadata.NamespaceProperty}/{pod.Metadata.Name} - {pod.Status.Phase}");
}

var response = await client.CustomObjects.ListClusterCustomObjectAsync(
    group: "metrics.k8s.io",
    version: "v1beta1",
    plural: "pods");

Console.WriteLine(response);