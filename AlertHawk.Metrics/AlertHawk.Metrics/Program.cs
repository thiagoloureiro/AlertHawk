using k8s;

var config = KubernetesClientConfiguration.InClusterConfig();
var client = new Kubernetes(config);

var namespacesToWatch = new[] { "alerthawk", "traefik", "ilstudio" };
foreach (var ns in namespacesToWatch)
{
    var pods = await client.CoreV1.ListNamespacedPodAsync(ns);

    foreach (var pod in pods.Items)
    {
        Console.WriteLine($"{pod.Metadata.NamespaceProperty}/{pod.Metadata.Name} - {pod.Status.Phase}");
    }

    var response = await client.CustomObjects.ListClusterCustomObjectAsync(
        group: "metrics.k8s.io",
        version: "v1beta1",
        plural: "pods");

    Console.WriteLine(response);
}